using System.Text;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Npgsql;

const string manifestSheetName = "_Manifest";
const string columnsSheetName = "_Columns";
const string nullToken = "__SKOLENZA_NULL_7D8B2F__";

var options = RestoreOptions.Parse(args);
if (!File.Exists(options.WorkbookPath))
    throw new FileNotFoundException("Seed workbook was not found.", options.WorkbookPath);
if (!File.Exists(options.SettingsPath))
    throw new FileNotFoundException("Database settings file was not found.", options.SettingsPath);

var connectionString = ReadConnection(options.SettingsPath);
using var workbook = new XLWorkbook(options.WorkbookPath);
ValidateWorkbookHeader(workbook);
var manifest = ReadManifest(workbook);
var workbookColumns = ReadWorkbookColumns(workbook);

await using var connection = new NpgsqlConnection(connectionString);
await connection.OpenAsync();
var liveColumns = await LoadLiveColumnsAsync(connection, options.Schema, manifest.Select(x => x.TableName).ToHashSet(StringComparer.OrdinalIgnoreCase));

ValidateWorkbook(workbook, manifest, workbookColumns, liveColumns);
Console.WriteLine($"Validated {manifest.Count} tables and {manifest.Sum(x => x.RowCount):N0} workbook rows against {connection.Database}.{options.Schema}.");

if (!options.Apply && !options.VerifyRestore)
{
    Console.WriteLine("Dry run complete. No database rows were changed. Add --apply to restore in one transaction.");
    return;
}

await using var transaction = await connection.BeginTransactionAsync();
try
{
    if (options.VerifyRestore)
    {
        await TruncateSchemaTablesAsync(connection, transaction, options.Schema);
        Console.WriteLine("Verification transaction prepared with an empty target schema.");
    }
    foreach (var table in manifest.OrderBy(x => x.RestoreOrder))
    {
        var columns = workbookColumns[table.TableName]
            .Where(x => !x.IsGenerated)
            .OrderBy(x => x.Ordinal)
            .ToList();
        var primaryKeys = table.PrimaryKey.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var sheet = workbook.Worksheet(table.SheetName);
        var restored = 0L;
        var nextRow = 2;
        while (restored < table.RowCount)
        {
            var count = (int)Math.Min(options.BatchSize, table.RowCount - restored);
            await RestoreBatchAsync(connection, transaction, options.Schema, table.TableName, sheet, nextRow, count, columns, primaryKeys);
            restored += count;
            nextRow += count;
        }
        await ResetSequencesAsync(connection, transaction, options.Schema, table.TableName, columns);
        Console.WriteLine($"{table.RestoreOrder,2}. {table.TableName}: {restored:N0} rows restored.");
    }
    if (options.VerifyRestore)
    {
        await transaction.RollbackAsync();
        Console.WriteLine("Restore verification completed successfully. All test writes were rolled back.");
    }
    else
    {
        await transaction.CommitAsync();
        Console.WriteLine("Seed workbook restore committed successfully.");
    }
}
catch
{
    await transaction.RollbackAsync();
    Console.Error.WriteLine("Restore failed. The transaction was rolled back; no partial restore was committed.");
    throw;
}

static void ValidateWorkbookHeader(XLWorkbook workbook)
{
    if (!workbook.TryGetWorksheet("_README", out var readme))
        throw new InvalidDataException("Workbook is missing _README.");
    if (!string.Equals(readme.Cell("B2").GetString(), "SKOLENZA_RENDER_SEED_WORKBOOK", StringComparison.Ordinal))
        throw new InvalidDataException("Workbook format marker is invalid.");
    if (!string.Equals(readme.Cell("B3").GetString(), "1", StringComparison.Ordinal))
        throw new InvalidDataException("Workbook format version is unsupported.");
    if (!string.Equals(readme.Cell("B5").GetString(), nullToken, StringComparison.Ordinal))
        throw new InvalidDataException("Workbook null-token marker is invalid.");
}

static List<TableManifest> ReadManifest(XLWorkbook workbook)
{
    var sheet = workbook.Worksheet(manifestSheetName);
    var headers = HeaderMap(sheet);
    var result = new List<TableManifest>();
    for (var row = 2; !sheet.Cell(row, headers["TableName"]).IsEmpty(); row++)
    {
        result.Add(new TableManifest(
            sheet.Cell(row, headers["TableName"]).GetString(),
            sheet.Cell(row, headers["SheetName"]).GetString(),
            sheet.Cell(row, headers["Kind"]).GetString(),
            sheet.Cell(row, headers["RestoreOrder"]).GetValue<int>(),
            sheet.Cell(row, headers["RowCount"]).GetValue<long>(),
            sheet.Cell(row, headers["PrimaryKey"]).GetString()));
    }
    return result;
}

static Dictionary<string, List<WorkbookColumn>> ReadWorkbookColumns(XLWorkbook workbook)
{
    var sheet = workbook.Worksheet(columnsSheetName);
    var headers = HeaderMap(sheet);
    var result = new Dictionary<string, List<WorkbookColumn>>(StringComparer.OrdinalIgnoreCase);
    for (var row = 2; !sheet.Cell(row, headers["TableName"]).IsEmpty(); row++)
    {
        var item = new WorkbookColumn(
            sheet.Cell(row, headers["TableName"]).GetString(),
            sheet.Cell(row, headers["Ordinal"]).GetValue<int>(),
            sheet.Cell(row, headers["ColumnName"]).GetString(),
            sheet.Cell(row, headers["DataType"]).GetString(),
            sheet.Cell(row, headers["UdtName"]).GetString(),
            bool.Parse(sheet.Cell(row, headers["IsNullable"]).GetString()),
            sheet.Cell(row, headers["IdentityGeneration"]).GetString(),
            bool.Parse(sheet.Cell(row, headers["IsGenerated"]).GetString()),
            sheet.Cell(row, headers["ColumnDefault"]).GetString());
        if (!result.TryGetValue(item.TableName, out var list)) result[item.TableName] = list = [];
        list.Add(item);
    }
    return result;
}

static Dictionary<string, int> HeaderMap(IXLWorksheet sheet)
{
    return sheet.Row(1).CellsUsed().ToDictionary(x => x.GetString(), x => x.Address.ColumnNumber, StringComparer.OrdinalIgnoreCase);
}

static async Task<Dictionary<string, Dictionary<string, LiveColumn>>> LoadLiveColumnsAsync(
    NpgsqlConnection connection, string schema, HashSet<string> tables)
{
    const string sql = """
        SELECT c.table_name, c.ordinal_position, c.column_name, c.data_type, c.udt_name,
               c.is_generated <> 'NEVER' AS is_generated,
               format_type(a.atttypid, a.atttypmod) AS format_type
        FROM information_schema.columns c
        JOIN pg_namespace n ON n.nspname = c.table_schema
        JOIN pg_class cl ON cl.relnamespace = n.oid AND cl.relname = c.table_name
        JOIN pg_attribute a ON a.attrelid = cl.oid AND a.attname = c.column_name AND a.attnum > 0
        WHERE c.table_schema = @schema
        ORDER BY c.table_name, c.ordinal_position;
        """;
    var result = new Dictionary<string, Dictionary<string, LiveColumn>>(StringComparer.OrdinalIgnoreCase);
    await using var command = new NpgsqlCommand(sql, connection);
    command.Parameters.AddWithValue("schema", schema);
    await using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        var table = reader.GetString(0);
        if (!tables.Contains(table)) continue;
        if (!result.TryGetValue(table, out var columns)) result[table] = columns = new(StringComparer.OrdinalIgnoreCase);
        columns[reader.GetString(2)] = new LiveColumn(reader.GetInt32(1), reader.GetString(3), reader.GetString(4), reader.GetBoolean(5), reader.GetString(6));
    }
    return result;
}

static void ValidateWorkbook(XLWorkbook workbook, List<TableManifest> manifest,
    Dictionary<string, List<WorkbookColumn>> workbookColumns,
    Dictionary<string, Dictionary<string, LiveColumn>> liveColumns)
{
    if (manifest.Count == 0) throw new InvalidDataException("Manifest contains no tables.");
    if (manifest.Select(x => x.TableName).Distinct(StringComparer.OrdinalIgnoreCase).Count() != manifest.Count)
        throw new InvalidDataException("Manifest contains duplicate table names.");
    if (manifest.Select(x => x.RestoreOrder).Distinct().Count() != manifest.Count)
        throw new InvalidDataException("Manifest contains duplicate restore-order values.");

    foreach (var table in manifest)
    {
        if (!workbook.TryGetWorksheet(table.SheetName, out var sheet))
            throw new InvalidDataException($"Missing worksheet {table.SheetName} for {table.TableName}.");
        if (!workbookColumns.TryGetValue(table.TableName, out var columns) || columns.Count == 0)
            throw new InvalidDataException($"Column metadata is missing for {table.TableName}.");
        if (!liveColumns.TryGetValue(table.TableName, out var live))
            throw new InvalidDataException($"Target table {table.TableName} does not exist.");
        var ordered = columns.OrderBy(x => x.Ordinal).ToList();
        for (var i = 0; i < ordered.Count; i++)
        {
            var column = ordered[i];
            if (!live.TryGetValue(column.ColumnName, out var target))
                throw new InvalidDataException($"Target column {table.TableName}.{column.ColumnName} does not exist.");
            if (!string.Equals(column.UdtName, target.UdtName, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"Column type mismatch for {table.TableName}.{column.ColumnName}: workbook {column.UdtName}, target {target.UdtName}.");
            if (!string.Equals(sheet.Cell(1, i + 1).GetString(), column.ColumnName, StringComparison.Ordinal))
                throw new InvalidDataException($"Worksheet header mismatch in {table.SheetName} column {i + 1}.");
        }
        var actualRows = Math.Max(0, sheet.LastRowUsed()?.RowNumber() - 1 ?? 0);
        if (actualRows != table.RowCount)
            throw new InvalidDataException($"Row-count mismatch for {table.TableName}: manifest {table.RowCount}, worksheet {actualRows}.");
    }
}

static async Task RestoreBatchAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, string schema,
    string table, IXLWorksheet sheet, int firstRow, int rowCount, List<WorkbookColumn> columns, string[] primaryKeys)
{
    var live = await LoadBatchColumnTypesAsync(connection, transaction, schema, table);
    var sql = new StringBuilder();
    sql.Append("INSERT INTO ").Append(Q(schema)).Append('.').Append(Q(table)).Append(" (")
       .Append(string.Join(',', columns.Select(x => Q(x.ColumnName)))).Append(')');
    if (columns.Any(x => !string.IsNullOrWhiteSpace(x.IdentityGeneration))) sql.Append(" OVERRIDING SYSTEM VALUE");
    sql.Append(" VALUES ");

    await using var command = new NpgsqlCommand { Connection = connection, Transaction = transaction, CommandTimeout = 300 };
    var parameterIndex = 0;
    for (var rowOffset = 0; rowOffset < rowCount; rowOffset++)
    {
        if (rowOffset > 0) sql.Append(',');
        sql.Append('(');
        for (var columnIndex = 0; columnIndex < columns.Count; columnIndex++)
        {
            if (columnIndex > 0) sql.Append(',');
            var value = sheet.Cell(firstRow + rowOffset, columns[columnIndex].Ordinal).GetString();
            if (string.Equals(value, nullToken, StringComparison.Ordinal))
            {
                sql.Append("NULL");
                continue;
            }
            var parameterName = $"p{parameterIndex++}";
            sql.Append("CAST(@").Append(parameterName).Append(" AS ").Append(live[columns[columnIndex].ColumnName]).Append(')');
            command.Parameters.AddWithValue(parameterName, value);
        }
        sql.Append(')');
    }

    if (primaryKeys.Length > 0)
    {
        sql.Append(" ON CONFLICT (").Append(string.Join(',', primaryKeys.Select(Q))).Append(") ");
        var updates = columns.Where(x => !primaryKeys.Contains(x.ColumnName, StringComparer.OrdinalIgnoreCase)).ToList();
        if (updates.Count == 0) sql.Append("DO NOTHING");
        else sql.Append("DO UPDATE SET ").Append(string.Join(',', updates.Select(x => $"{Q(x.ColumnName)}=EXCLUDED.{Q(x.ColumnName)}")));
    }
    else sql.Append(" ON CONFLICT DO NOTHING");
    command.CommandText = sql.ToString();
    await command.ExecuteNonQueryAsync();
}

static async Task<Dictionary<string, string>> LoadBatchColumnTypesAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, string schema, string table)
{
    const string sql = """
        SELECT a.attname, format_type(a.atttypid, a.atttypmod)
        FROM pg_attribute a
        JOIN pg_class c ON c.oid = a.attrelid
        JOIN pg_namespace n ON n.oid = c.relnamespace
        WHERE n.nspname = @schema AND c.relname = @table AND a.attnum > 0 AND NOT a.attisdropped;
        """;
    var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    await using var command = new NpgsqlCommand(sql, connection, transaction);
    command.Parameters.AddWithValue("schema", schema);
    command.Parameters.AddWithValue("table", table);
    await using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync()) result[reader.GetString(0)] = reader.GetString(1);
    return result;
}

static async Task ResetSequencesAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, string schema,
    string table, List<WorkbookColumn> columns)
{
    foreach (var column in columns.Where(x => !string.IsNullOrWhiteSpace(x.IdentityGeneration) || x.ColumnDefault.Contains("nextval", StringComparison.OrdinalIgnoreCase)))
    {
        var sql = $"""
            SELECT setval(pg_get_serial_sequence(@qualified, @column),
                          COALESCE((SELECT MAX({Q(column.ColumnName)}) FROM {Q(schema)}.{Q(table)}), 1),
                          EXISTS(SELECT 1 FROM {Q(schema)}.{Q(table)}));
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("qualified", $"{Q(schema)}.{Q(table)}");
        command.Parameters.AddWithValue("column", column.ColumnName);
        var sequence = await command.ExecuteScalarAsync();
        _ = sequence;
    }
}

static async Task TruncateSchemaTablesAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, string schema)
{
    const string tableSql = """
        SELECT tablename
        FROM pg_tables
        WHERE schemaname = @schema
        ORDER BY tablename;
        """;
    var tables = new List<string>();
    await using (var tableCommand = new NpgsqlCommand(tableSql, connection, transaction))
    {
        tableCommand.Parameters.AddWithValue("schema", schema);
        await using var reader = await tableCommand.ExecuteReaderAsync();
        while (await reader.ReadAsync()) tables.Add(reader.GetString(0));
    }
    if (tables.Count == 0) throw new InvalidOperationException($"Schema {schema} contains no tables.");
    var truncateSql = $"TRUNCATE TABLE {string.Join(',', tables.Select(x => $"{Q(schema)}.{Q(x)}"))} RESTART IDENTITY CASCADE";
    await using var truncate = new NpgsqlCommand(truncateSql, connection, transaction) { CommandTimeout = 300 };
    await truncate.ExecuteNonQueryAsync();
}

static string Q(string identifier) => $"\"{identifier.Replace("\"", "\"\"")}\"";

static string ReadConnection(string path)
{
    var text = File.ReadAllText(path);
    var match = Regex.Match(text, "(?m)^\\s*\\\"DefaultConnection\\\"\\s*:\\s*\\\"([^\\\"]+)\\\"");
    if (!match.Success) throw new InvalidDataException("DefaultConnection was not found in the settings file.");
    return match.Groups[1].Value;
}

internal sealed record TableManifest(string TableName, string SheetName, string Kind, int RestoreOrder, long RowCount, string PrimaryKey);
internal sealed record WorkbookColumn(string TableName, int Ordinal, string ColumnName, string DataType, string UdtName,
    bool IsNullable, string IdentityGeneration, bool IsGenerated, string ColumnDefault);
internal sealed record LiveColumn(int Ordinal, string DataType, string UdtName, bool IsGenerated, string FormatType);

internal sealed record RestoreOptions(string WorkbookPath, string SettingsPath, string Schema, bool Apply, bool VerifyRestore, int BatchSize)
{
    public static RestoreOptions Parse(string[] args)
    {
        string? file = null;
        string? settings = null;
        var schema = "skolenza";
        var apply = false;
        var verifyRestore = false;
        var batchSize = 500;
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--file": file = RequireValue(args, ref i); break;
                case "--settings": settings = RequireValue(args, ref i); break;
                case "--schema": schema = RequireValue(args, ref i); break;
                case "--batch-size": batchSize = int.Parse(RequireValue(args, ref i)); break;
                case "--apply": apply = true; break;
                case "--verify-restore": verifyRestore = true; break;
                default: throw new ArgumentException($"Unknown option: {args[i]}");
            }
        }
        if (string.IsNullOrWhiteSpace(file) || string.IsNullOrWhiteSpace(settings))
            throw new ArgumentException("Usage: --file <xlsx> --settings <appsettings.json> [--schema skolenza] [--batch-size 500] [--apply | --verify-restore]");
        if (apply && verifyRestore) throw new ArgumentException("Choose either --apply or --verify-restore, not both.");
        if (batchSize is < 1 or > 1000) throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size must be 1-1000.");
        return new RestoreOptions(Path.GetFullPath(file), Path.GetFullPath(settings), schema, apply, verifyRestore, batchSize);
    }

    private static string RequireValue(string[] args, ref int index)
    {
        if (++index >= args.Length) throw new ArgumentException("Option value is missing.");
        return args[index];
    }
}
