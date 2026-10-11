using Npgsql;
using NUnit.Framework;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("DuplicateExportOperationCleanup")]
public sealed class DuplicateExportOperationCleanupTests
{
    [Test]
    public void Seeds_define_only_the_canonical_export_and_do_not_contain_cleanup_logic()
    {
        var root = FindRepositoryRoot();
        foreach (var path in new[]
        {
            Path.Combine(root, "database-scripts", "SeedBulkImportModules.sql"),
            Path.Combine(root, "database-scripts", "complete seed data", "Skolenza_New_Production_Module_Operation_Seed.sql")
        })
        {
            var sql = File.ReadAllText(path);
            Assert.Multiple(() =>
            {
                Assert.That(sql, Does.Contain("WHERE \"OperationType\" = 11"), path);
                Assert.That(sql, Does.Contain("'Export'"), path);
                Assert.That(sql, Does.Contain("'Export authorized module data for spreadsheet use.'"), path);
                Assert.That(sql, Does.Not.Contain("REMOVE DUPLICATE EXPORT OPERATIONS"), path);
                Assert.That(sql, Does.Not.Contain("duplicate_export_id"), path);
            });
        }

        var policySeeds = new[]
        {
            Path.Combine(root, "database-scripts", "complete seed data", "SeedInstitutionPolicyModules.sql"),
            Path.Combine(root, "database-scripts", "complete seed data", "Skolenza_New_Production_Module_Operation_Seed.sql")
        };
        foreach (var path in policySeeds)
        {
            var sql = File.ReadAllText(path);
            Assert.That(sql, Does.Contain("\"OperationName\" varchar(100) PRIMARY KEY"), path);
            Assert.That(sql, Does.Contain("PRIMARY KEY (\"ModuleCode\", \"OperationName\")"), path);
        }

        var staffEmploymentTypeSeed = File.ReadAllText(Path.Combine(
            root, "database-scripts", "SeedInstitutionStaffEmploymentTypeModule.sql"));
        Assert.That(staffEmploymentTypeSeed, Does.Not.Contain("second operation named Export"));

        foreach (var relativePath in new[]
        {
            Path.Combine("database-scripts", "SeedBulkImportModules.sql"),
            Path.Combine("database-scripts", "SeedHostBulkImportModules.sql"),
            Path.Combine("database-scripts", "SeedInstitutionStaffEmploymentTypeModule.sql")
        })
        {
            var sql = File.ReadAllText(Path.Combine(root, relativePath));
            Assert.That(sql, Does.Not.Contain("DELETE FROM skolenza.\"Module\""), relativePath);
            Assert.That(sql, Does.Not.Contain("DELETE FROM skolenza.\"Operation\""), relativePath);
            Assert.That(sql, Does.Not.Contain("DELETE FROM skolenza.\"ModuleOperationMapping\""), relativePath);
            Assert.That(sql, Does.Not.Contain("DELETE FROM skolenza.\"InstitutionEnabledOperation\""), relativePath);
        }
    }

    [Test]
    public async Task Configured_database_has_one_canonical_export_and_no_operation_14_dependencies()
    {
        var connectionString = Environment.GetEnvironmentVariable("SKOLENZA_MODULE_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Assert.Ignore("Set SKOLENZA_MODULE_TEST_CONNECTION to run the PostgreSQL state check.");
        }

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                (SELECT count(*) FROM skolenza."Operation"
                 WHERE lower(btrim("OperationName"))='export'),
                (SELECT count(*) FROM skolenza."Operation"
                 WHERE "Id"=23 AND "OperationType"=11 AND "IsActive"),
                (SELECT count(*) FROM skolenza."ModuleOperationMapping" WHERE "OperationId"=14),
                (SELECT count(*) FROM skolenza."InstitutionEnabledOperation" WHERE "OperationId"=14),
                (SELECT count(*) FROM skolenza."RoleModuleAndPermission" WHERE "OperationId"=14),
                (SELECT count(*) FROM skolenza."HostRoleModuleAndPermission" WHERE "OperationId"=14);
            """;

        await using var reader = await command.ExecuteReaderAsync();
        Assert.That(await reader.ReadAsync(), Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(reader.GetInt64(0), Is.EqualTo(1));
            Assert.That(reader.GetInt64(1), Is.EqualTo(1));
            Assert.That(reader.GetInt64(2), Is.Zero);
            Assert.That(reader.GetInt64(3), Is.Zero);
            Assert.That(reader.GetInt64(4), Is.Zero);
            Assert.That(reader.GetInt64(5), Is.Zero);
        });
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Skolenza.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate Skolenza.sln.");
    }
}
