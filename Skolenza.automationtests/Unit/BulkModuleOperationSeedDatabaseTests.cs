using Npgsql;
using NUnit.Framework;

namespace skolenza.automationtests.Unit;

/// <summary>
/// Verifies bulk actions are attached to existing functional modules and the
/// obsolete navigation-only bulk Module rows have been removed.
/// </summary>
[TestFixture]
[Category("BulkModuleOperationSeed")]
[NonParallelizable]
public sealed class BulkModuleOperationSeedDatabaseTests
{
    private NpgsqlConnection _connection = null!;

    [SetUp]
    public async Task SetUp()
    {
        var connectionString = Environment.GetEnvironmentVariable("SKOLENZA_BULK_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Assert.Ignore("Requires disposable skolenza_bulk_test database.");
        }

        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        Assert.Multiple(() =>
        {
            Assert.That(builder.Database, Is.EqualTo("skolenza_bulk_test"));
            Assert.That(builder.Host, Is.AnyOf("127.0.0.1", "localhost"));
        });

        _connection = new NpgsqlConnection(connectionString);
        await _connection.OpenAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
    }

    [Test]
    public async Task Existing_modules_own_import_export_and_bulk_children_are_absent()
    {
        var obsoleteModules = await ScalarAsync("""
            SELECT count(*)
            FROM skolenza."Module"
            WHERE "ModuleCode" LIKE 'BULK\_%' ESCAPE '\'
               OR "ModuleCode" LIKE 'HOST%\_BULK' ESCAPE '\'
               OR "ModuleCode" = 'BULKUPLOAD';
            """);

        var expectedMappings = await ScalarAsync("""
            SELECT count(*)
            FROM skolenza."Module" module
            JOIN skolenza."ModuleOperationMapping" mapping
              ON mapping."ModuleId"=module."Id" AND mapping."IsActive"
            JOIN skolenza."Operation" operation
              ON operation."Id"=mapping."OperationId" AND operation."IsActive"
            WHERE module."ModuleCode" IN
                ('EMP_LIST','DEPARTMENT','ROLE','STAFF_MEMBER_TYPE',
                 'HOST_INSTITUTION_RFID_MANAGEMENT','HOST_DEVICE_SETUP','HOST_MODULES',
                 'HOST_SUBMODULES','HOST_OPERATIONS','HOST_MODULE_OPERATIONS')
              AND operation."OperationType" IN (11,12);
            """);

        var duplicateMappings = await ScalarAsync("""
            SELECT count(*) FROM (
                SELECT "ModuleId","OperationId"
                FROM skolenza."ModuleOperationMapping"
                GROUP BY "ModuleId","OperationId"
                HAVING count(*) > 1
            ) duplicate;
            """);

        Assert.Multiple(() =>
        {
            Assert.That(obsoleteModules, Is.Zero);
            Assert.That(expectedMappings, Is.EqualTo(20));
            Assert.That(duplicateMappings, Is.Zero);
        });
    }

    private async Task<long> ScalarAsync(string sql)
    {
        await using var command = new NpgsqlCommand(sql, _connection);
        return Convert.ToInt64(await command.ExecuteScalarAsync());
    }
}
