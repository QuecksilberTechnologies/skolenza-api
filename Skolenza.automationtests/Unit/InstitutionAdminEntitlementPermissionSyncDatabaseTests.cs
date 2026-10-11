using skolenza.application.Constants;
using skolenza.persistance.Data.Context;
using skolenza.persistance.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using NUnit.Framework;

namespace skolenza.automationtests.Unit;

/// <summary>
/// Verifies that Host entitlement synchronization grants only missing enabled operations
/// to the Institution Admin role without creating duplicate permission rows.
/// </summary>
[TestFixture]
[Category("InstitutionEntitlementPermissionSyncDatabase")]
[NonParallelizable]
public sealed class InstitutionAdminEntitlementPermissionSyncDatabaseTests
{
    [Test]
    public void Entitlement_command_persists_snapshot_before_missing_admin_permission_sync()
    {
        var repositoryRoot = FindRepositoryRoot();
        var commandSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "skolenza.application",
            "Features",
            "InstitutionManagementCmd",
            "Commands",
            "SynchronizeInstitutionPlanEntitlementsCommand.cs"));
        var repositorySource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "skolenza.persistance",
            "Repositories",
            "UserRolesPermissionOnModuleRepository.cs"));

        var firstSave = commandSource.IndexOf(
            "SaveChangesAsync(cancellationToken)",
            StringComparison.Ordinal);
        var permissionSync = commandSource.IndexOf(
            "AddMissingInstitutionAdminPermissionsAsync",
            StringComparison.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(firstSave, Is.GreaterThanOrEqualTo(0));
            Assert.That(permissionSync, Is.GreaterThan(firstSave));
            Assert.That(repositorySource, Does.Contain("operation.InstitutionId == institutionId && operation.IsEnabled"));
            Assert.That(repositorySource, Does.Contain("permission.RoleId == adminRole.Id"));
            Assert.That(repositorySource, Does.Contain("!existingPermissionKeys.Contains"));
            Assert.That(repositorySource, Does.Contain("RoleType == ConstantValues.RoleTypeAdmin"));
        });
    }

    [Test]
    public async Task Missing_admin_permissions_are_added_once_and_existing_rows_are_unchanged()
    {
        var connection = Environment.GetEnvironmentVariable("SKOLENZA_BULK_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(connection))
        {
            Assert.Ignore("Requires disposable skolenza_bulk_test database.");
        }

        var connectionBuilder = new NpgsqlConnectionStringBuilder(connection);
        Assert.Multiple(() =>
        {
            Assert.That(connectionBuilder.Database, Is.EqualTo("skolenza_bulk_test"));
            Assert.That(connectionBuilder.Host, Is.AnyOf("127.0.0.1", "localhost"));
        });

        var options = new DbContextOptionsBuilder<SkolenzaDbContext>()
            .UseNpgsql(connection)
            .Options;

        await using var context = new SkolenzaDbContext(options);
        await using var transaction = await context.Database.BeginTransactionAsync();

        var adminRole = await context.Roles
            .AsNoTracking()
            .Where(role =>
                role.InstitutionId.HasValue &&
                role.RoleType == ConstantValues.RoleTypeAdmin &&
                role.IsActive &&
                role.IsSoftDeleted == false &&
                role.IsSystemDefault == false)
            .Where(role => context.InstitutionEnabledOperations.Any(operation =>
                operation.InstitutionId == role.InstitutionId && operation.IsEnabled))
            .FirstOrDefaultAsync();

        Assert.That(adminRole, Is.Not.Null, "The disposable database needs an active Institution Admin with an enabled operation.");
        var institutionId = adminRole!.InstitutionId!.Value;

        var enabledOperation = await context.InstitutionEnabledOperations
            .AsNoTracking()
            .Where(operation => operation.InstitutionId == institutionId && operation.IsEnabled)
            .Select(operation => new { operation.ModuleId, operation.OperationId })
            .FirstAsync();

        await context.RoleModuleAndPermissions
            .Where(permission =>
                permission.RoleId == adminRole.Id &&
                permission.ModuleId == enabledOperation.ModuleId &&
                permission.OperationId == enabledOperation.OperationId)
            .ExecuteDeleteAsync();

        var repository = new UserRolesPermissionOnModuleRepository(
            context,
            NullLogger<UserRolesPermissionOnModuleRepository>.Instance);

        var firstAddedCount = await repository.AddMissingInstitutionAdminPermissionsAsync(
            institutionId,
            adminRole.AddedById);
        await context.SaveChangesAsync();

        var matchingCountAfterFirstSync = await context.RoleModuleAndPermissions.CountAsync(permission =>
            permission.RoleId == adminRole.Id &&
            permission.ModuleId == enabledOperation.ModuleId &&
            permission.OperationId == enabledOperation.OperationId);

        var secondAddedCount = await repository.AddMissingInstitutionAdminPermissionsAsync(
            institutionId,
            adminRole.AddedById);
        await context.SaveChangesAsync();

        var matchingCountAfterSecondSync = await context.RoleModuleAndPermissions.CountAsync(permission =>
            permission.RoleId == adminRole.Id &&
            permission.ModuleId == enabledOperation.ModuleId &&
            permission.OperationId == enabledOperation.OperationId);

        Assert.Multiple(() =>
        {
            Assert.That(firstAddedCount, Is.GreaterThanOrEqualTo(1));
            Assert.That(matchingCountAfterFirstSync, Is.EqualTo(1));
            Assert.That(secondAddedCount, Is.EqualTo(0));
            Assert.That(matchingCountAfterSecondSync, Is.EqualTo(1));
        });

        await transaction.RollbackAsync();
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Skolenza.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Repository root containing Skolenza.sln was not found.");
    }
}
