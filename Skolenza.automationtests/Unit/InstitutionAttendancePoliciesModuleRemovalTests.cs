using Npgsql;
using NUnit.Framework;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("InstitutionAttendancePoliciesModuleRemoval")]
public sealed class InstitutionAttendancePoliciesModuleRemovalTests
{
    [Test]
    public void Production_seed_does_not_define_attendance_policies_module()
    {
        var seed = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(), "database-scripts", "complete seed data",
            "Skolenza_New_Production_Module_Operation_Seed.sql"));

        Assert.Multiple(() =>
        {
            Assert.That(seed, Does.Not.Contain("INSTITUTION_ATTENDANCE_POLICIES"));
            Assert.That(seed, Does.Not.Contain("Institution Attendance Policies"));
            Assert.That(seed, Does.Not.Contain("/app/attendance-policies"));
            Assert.That(seed, Does.Contain("Expected nine active Institution feature modules"));
        });
    }

    [Test]
    public async Task Configured_database_has_no_module_67_dependencies()
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
              (SELECT count(*) FROM skolenza."Module" WHERE "Id"=67 OR "ModuleCode"='INSTITUTION_ATTENDANCE_POLICIES'),
              (SELECT count(*) FROM skolenza."ModuleOperationMapping" WHERE "ModuleId"=67),
              (SELECT count(*) FROM skolenza."InstitutionEnabledModule" WHERE "ModuleId"=67),
              (SELECT count(*) FROM skolenza."InstitutionEnabledOperation" WHERE "ModuleId"=67),
              (SELECT count(*) FROM skolenza."PlanModuleMapping" WHERE "ModuleId"=67),
              (SELECT count(*) FROM skolenza."RoleModuleAndPermission" WHERE "ModuleId"=67),
              (SELECT count(*) FROM skolenza."HostRoleModuleAndPermission" WHERE "ModuleId"=67);
            """;

        await using var reader = await command.ExecuteReaderAsync();
        Assert.That(await reader.ReadAsync(), Is.True);
        for (var index = 0; index < 7; index++)
        {
            Assert.That(reader.GetInt64(index), Is.Zero, $"Dependency result {index} must be zero.");
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Skolenza.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate Skolenza.sln.");
    }
}
