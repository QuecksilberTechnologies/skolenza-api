using System.IO.Compression;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using skolenza.application.Constants;
using skolenza.application.DTOS.Common;
using skolenza.application.Exceptions;
using Microsoft.VisualBasic.FileIO;

namespace skolenza.application.Common.Helpers;

/// <summary>Reads bounded CSV, pasted TSV and XLSX tables without evaluating formulas or external links.</summary>
public static class BulkImportTableReader
{
    #region Input validation

    public static async Task<BulkImportTableDTO> ReadAsync(
        BulkImportPreviewRequestDTO request,
        CancellationToken cancellationToken)
    {
        if ((request.File is null) == string.IsNullOrWhiteSpace(request.PastedText))
        {
            throw new ValidationErrorException("Supply exactly one file or pasted table.");
        }

        if (request.File is null)
        {
            if (Encoding.UTF8.GetByteCount(request.PastedText!) > BulkImportConstants.MaxFileBytes)
            {
                throw new ValidationErrorException("Pasted table exceeds the 5 MB limit.");
            }

            return ReadDelimited(request.PastedText!, cancellationToken);
        }

        var extension = Path.GetExtension(request.File.FileName).ToLowerInvariant();
        if (extension is not ".csv" and not ".xlsx")
        {
            throw new ValidationErrorException("Upload an .xlsx or UTF-8 .csv file.");
        }

        if (request.File.Length <= 0 || request.File.Length > BulkImportConstants.MaxFileBytes)
        {
            throw new ValidationErrorException("File must contain data and must not exceed 5 MB.");
        }

        using var input = request.File.OpenReadStream();
        using var buffer = new MemoryStream();
        var chunk = new byte[8192];
        int count;
        while ((count = await input.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (buffer.Length + count > BulkImportConstants.MaxFileBytes)
            {
                throw new ValidationErrorException("File exceeds the 5 MB limit.");
            }

            await buffer.WriteAsync(chunk.AsMemory(0, count), cancellationToken);
        }

        buffer.Position = 0;
        try
        {
            if (extension == ".xlsx")
            {
                return ReadWorkbook(buffer, request.SheetName, cancellationToken);
            }

            var text = new UTF8Encoding(false, true).GetString(buffer.ToArray()).TrimStart('\uFEFF');
            return ReadDelimited(text, cancellationToken);
        }
        catch (Exception ex) when (ex is InvalidDataException or XmlException or DecoderFallbackException)
        {
            throw new ValidationErrorException("The file is not a supported, readable workbook or UTF-8 CSV.");
        }
    }

    #endregion

    #region Delimited input

    private static BulkImportTableDTO ReadDelimited(string text, CancellationToken cancellationToken)
    {
        using var reader = new StringReader(text);
        using var parser = new TextFieldParser(reader);
        parser.SetDelimiters(text.Split('\n')[0].Contains('\t') ? "\t" : ",");
        parser.HasFieldsEnclosedInQuotes = true;
        parser.TrimWhiteSpace = true;
        var table = new BulkImportTableDTO();

        try
        {
            while (!parser.EndOfData)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var rowNumber = checked((int)parser.LineNumber);
                var fields = parser.ReadFields() ?? Array.Empty<string>();
                AddRow(table, rowNumber, fields);
            }
        }
        catch (MalformedLineException)
        {
            throw new ValidationErrorException("CSV contains malformed quoted fields.");
        }

        EnsureData(table);
        return table;
    }

    #endregion

    #region Workbook input

    private static BulkImportTableDTO ReadWorkbook(
        Stream input,
        string? sheetName,
        CancellationToken cancellationToken)
    {
        using var archive = new ZipArchive(input, ZipArchiveMode.Read, leaveOpen: true);
        if (archive.Entries.Count > 1000 ||
            archive.Entries.Sum(entry => entry.Length) > BulkImportConstants.MaxExpandedBytes)
        {
            throw new ValidationErrorException("Workbook exceeds the expanded size or entry limit.");
        }

        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        XNamespace relationships = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        var workbook = ReadXml(archive, "xl/workbook.xml");
        var sheets = workbook.Descendants(ns + "sheet").ToList();
        var selected = string.IsNullOrWhiteSpace(sheetName)
            ? (sheets.Count == 1 ? sheets[0] : null)
            : sheets.SingleOrDefault(sheet => (string?)sheet.Attribute("name") == sheetName);
        if (selected is null)
        {
            throw new ValidationErrorException("Select a worksheet: " +
                string.Join(", ", sheets.Select(sheet => (string?)sheet.Attribute("name"))));
        }

        var id = (string?)selected.Attribute(relationships + "id");
        var relation = ReadXml(archive, "xl/_rels/workbook.xml.rels")
            .Root?.Elements().SingleOrDefault(item => (string?)item.Attribute("Id") == id);
        var target = (string?)relation?.Attribute("Target");
        if (string.IsNullOrWhiteSpace(target) ||
            (string?)relation?.Attribute("TargetMode") == "External" ||
            target.Contains("..", StringComparison.Ordinal) || target.Contains('\\'))
        {
            throw new ValidationErrorException("Workbook worksheet reference is invalid.");
        }

        var path = target.StartsWith('/') ? target.TrimStart('/') : "xl/" + target;
        var shared = archive.GetEntry("xl/sharedStrings.xml") is null
            ? new List<string>()
            : ReadXml(archive, "xl/sharedStrings.xml").Descendants(ns + "si")
                .Select(item => string.Concat(item.Descendants(ns + "t").Select(value => value.Value)))
                .ToList();
        var sheetDocument = ReadXml(archive, path);
        if (sheetDocument.Descendants(ns + "mergeCell").Any())
        {
            throw new ValidationErrorException("Unmerge cells in the selected worksheet before importing.");
        }

        var table = new BulkImportTableDTO
        {
            IsExcel = true,
            Uses1904DateSystem = (string?)workbook.Root?.Element(ns + "workbookPr")?.Attribute("date1904") is "1" or "true"
        };
        foreach (var row in sheetDocument.Descendants(ns + "sheetData").Elements(ns + "row"))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!int.TryParse((string?)row.Attribute("r"), out var rowNumber) || rowNumber <= 0)
            {
                throw new ValidationErrorException("Worksheet row number is invalid.");
            }

            var cells = new SortedDictionary<int, string>();
            foreach (var cell in row.Elements(ns + "c"))
            {
                if (cell.Element(ns + "f") is not null)
                {
                    throw new ValidationErrorException($"Row {rowNumber}: replace formulas with values before importing.");
                }

                var reference = (string?)cell.Attribute("r") ?? string.Empty;
                var index = 0;
                foreach (var letter in reference.TakeWhile(char.IsAsciiLetterUpper))
                {
                    index = index * 26 + letter - 'A' + 1;
                    if (index > BulkImportConstants.MaxColumns)
                    {
                        throw new ValidationErrorException("Workbook exceeds the 64-column limit.");
                    }
                }

                if (index == 0 || cells.ContainsKey(index))
                {
                    throw new ValidationErrorException("Worksheet contains an invalid or repeated cell reference.");
                }

                var type = (string?)cell.Attribute("t");
                var value = cell.Element(ns + "v")?.Value ?? string.Empty;
                if (type == "s")
                {
                    if (!int.TryParse(value, out var sharedIndex) || sharedIndex < 0 || sharedIndex >= shared.Count)
                    {
                        throw new ValidationErrorException("Workbook shared-string reference is invalid.");
                    }

                    value = shared[sharedIndex];
                }
                else if (type == "inlineStr")
                {
                    value = string.Concat(cell.Descendants(ns + "t").Select(item => item.Value));
                }
                else if (type == "b")
                {
                    value = value == "1" ? "true" : "false";
                }
                else if (type == "e")
                {
                    throw new ValidationErrorException($"Row {rowNumber}: correct the Excel cell error.");
                }

                cells.Add(index, value);
            }

            var width = Math.Max(table.Columns.Count, cells.Count == 0 ? 0 : cells.Keys.Max());
            AddRow(table, rowNumber, Enumerable.Range(1, width)
                .Select(index => cells.GetValueOrDefault(index, string.Empty)).ToArray());
        }

        EnsureData(table);
        return table;
    }

    private static XDocument ReadXml(ZipArchive archive, string path)
    {
        var entry = archive.GetEntry(path)
            ?? throw new ValidationErrorException("Required workbook content is missing.");
        using var stream = entry.Open();
        using var reader = XmlReader.Create(stream, new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = BulkImportConstants.MaxExpandedBytes
        });
        return XDocument.Load(reader);
    }

    #endregion

    #region Table validation

    private static void AddRow(BulkImportTableDTO table, int rowNumber, string[] fields)
    {
        if (fields.Length > BulkImportConstants.MaxColumns ||
            fields.Any(value => value.Length > BulkImportConstants.MaxCellLength))
        {
            throw new ValidationErrorException("Table exceeds the column or cell-length limit.");
        }

        if (fields.All(string.IsNullOrWhiteSpace))
        {
            return;
        }

        if (table.Columns.Count == 0)
        {
            var headers = fields.Select(value => value.Trim()).ToList();
            if (headers.Any(string.IsNullOrWhiteSpace) ||
                headers.Distinct(StringComparer.OrdinalIgnoreCase).Count() != headers.Count)
            {
                throw new ValidationErrorException("Column headers must be non-empty and unique.");
            }

            table.Columns = headers;
            return;
        }

        if (fields.Length != table.Columns.Count)
        {
            throw new ValidationErrorException($"Row {rowNumber}: column count does not match the header.");
        }

        if (table.Rows.Count >= BulkImportConstants.MaxRows)
        {
            throw new ValidationErrorException("Import exceeds the 5000-row limit.");
        }

        table.Rows.Add(new BulkImportSourceRowDTO
        {
            RowNumber = rowNumber,
            Values = fields.Select(value => value.Trim()).ToList()
        });
    }

    private static void EnsureData(BulkImportTableDTO table)
    {
        if (table.Columns.Count == 0 || table.Rows.Count == 0)
        {
            throw new ValidationErrorException("Provide a header row and at least one data row.");
        }
    }

    #endregion
}
