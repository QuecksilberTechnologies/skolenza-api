using System.Reflection;
using skolenza.application.Features.StaffMemberCmd;
using skolenza.application.Features.StaffMemberCmd.ResetPassword.Handlers;
using Npgsql;
using NUnit.Framework;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("ResetPasswordModuleConsolidation")]
public sealed class ResetPasswordModuleConsolidationTests
{
    [Test]
    public void Reset_password_request_is_authorized_by_staffMember_list_module()
    {
        var behaviorType = typeof(StaffMemberInstitutionPermissionBehavior<,>)
            .MakeGenericType(typeof(ResetPasswordCommand), typeof(object));
        var resolver = behaviorType.GetMethod(
            "ResolveExpectedModuleCode",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new AssertionException("StaffMember permission module resolver was not found.");

        Assert.That(resolver.Invoke(null, null), Is.EqualTo("STAFF_DIRECTORY"));
    }

    [Test]
    public void Migration_and_seed_remove_module_38_and_map_operation_21_to_staffMember_list()
    {
        var root = FindRepositoryRoot();
        var migration = File.ReadAllText(Path.Combine(
            root, "database-scripts", "MoveResetPasswordToStaffMemberList.sql"));
        var seed = File.ReadAllText(Path.Combine(
            root, "database-scripts", "complete seed data",
            "Skolenza_New_Production_Module_Operation_Seed.sql"));

        Assert.Multiple(() =>
        {
            Assert.That(migration, Does.Contain("Operation Id 21 must be Reset Password"));
            Assert.That(migration, Does.Contain("module.\"ModuleCode\" = 'STAFF_DIRECTORY'"));
            Assert.That(migration, Does.Contain("DELETE FROM skolenza.\"InstitutionEnabledOperation\" WHERE \"ModuleId\" = 38"));
            Assert.That(migration, Does.Contain("DELETE FROM skolenza.\"InstitutionEnabledModule\" WHERE \"ModuleId\" = 38"));
            Assert.That(migration, Does.Contain("DELETE FROM skolenza.\"ModuleOperationMapping\" WHERE \"ModuleId\" = 38"));
            Assert.That(seed, Does.Contain("RESET PASSWORD OPERATION ON STAFF_DIRECTORY"));
            Assert.That(seed, Does.Not.Contain("'StaffMember-Password-Management'"));
            Assert.That(seed, Does.Not.Contain("WHERE legacy.\"ModuleId\" = 38"));
        });
    }

    [Test]
    public async Task Configured_database_has_no_module_38_and_staffMember_list_owns_reset_password()
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
                (SELECT count(*) FROM skolenza."Module"
                 WHERE "Id" = 38 OR "ModuleCode" = 'EMP_PASSWORD_MANAGEMENT'),
                (SELECT count(*) FROM skolenza."ModuleOperationMapping"
                 WHERE "ModuleId" = (SELECT "Id" FROM skolenza."Module" WHERE "ModuleCode" = 'STAFF_DIRECTORY')
                   AND "OperationId" = 21 AND "IsActive" = true),
                (SELECT count(*) FROM skolenza."InstitutionEnabledModule" WHERE "ModuleId" = 38),
                (SELECT count(*) FROM skolenza."InstitutionEnabledOperation" WHERE "ModuleId" = 38),
                (SELECT count(*) FROM skolenza."RoleModuleAndPermission" WHERE "ModuleId" = 38),
                (SELECT count(*) FROM skolenza."PlanModuleMapping" WHERE "ModuleId" = 38),
                (SELECT count(*) FROM skolenza."Operation"
                 WHERE "Id" = 21 AND "OperationName" = 'Reset Password' AND "IsActive" = true),
                (SELECT count(*) FROM skolenza."InstitutionEnabledOperation"
                 WHERE "ModuleId" = (SELECT "Id" FROM skolenza."Module" WHERE "ModuleCode" = 'STAFF_DIRECTORY')
                   AND "OperationId" = 21),
                (SELECT count(*) FROM skolenza."RoleModuleAndPermission"
                 WHERE "ModuleId" = (SELECT "Id" FROM skolenza."Module" WHERE "ModuleCode" = 'STAFF_DIRECTORY')
                   AND "OperationId" = 21 AND "IsSoftDeleted" IS DISTINCT FROM true);
            """;

        await using var reader = await command.ExecuteReaderAsync();
        Assert.That(await reader.ReadAsync(), Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(reader.GetInt64(0), Is.Zero, "Module 38 must be absent.");
            Assert.That(reader.GetInt64(1), Is.EqualTo(1), "STAFF_DIRECTORY must own one active Reset Password mapping.");
            Assert.That(reader.GetInt64(2), Is.Zero);
            Assert.That(reader.GetInt64(3), Is.Zero);
            Assert.That(reader.GetInt64(4), Is.Zero);
            Assert.That(reader.GetInt64(5), Is.Zero);
            Assert.That(reader.GetInt64(6), Is.EqualTo(1), "Operation Id 21 must remain active.");
            Assert.That(reader.GetInt64(7), Is.GreaterThanOrEqualTo(1), "Institution entitlement must be on STAFF_DIRECTORY.");
            Assert.That(reader.GetInt64(8), Is.GreaterThanOrEqualTo(1), "Role grant must be on STAFF_DIRECTORY.");
        });
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Skolenza.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate Skolenza.sln.");
    }
}
