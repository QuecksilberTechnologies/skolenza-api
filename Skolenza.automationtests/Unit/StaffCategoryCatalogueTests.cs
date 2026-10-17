using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Routing;
using NUnit.Framework;
using skolenza.api.Controllers.Staff;
using skolenza.application.Constants;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("StaffCategoryCatalogue")]
public sealed class StaffCategoryCatalogueTests
{
    [Test]
    [Category("StaffCategoryCatalogueDatabase")]
    public async Task Host_managed_category_allows_extended_ids_but_rejects_unknown_categories()
    {
        var settingsPath = Environment.GetEnvironmentVariable("SKOLENZA_HOST_DB_SETTINGS");
        if (string.IsNullOrWhiteSpace(settingsPath))
        {
            Assert.Ignore("Opt-in PostgreSQL rollback test.");
        }

        var configuration = new ConfigurationBuilder().AddJsonFile(settingsPath!).Build();
        await using var connection = new NpgsqlConnection(configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        try
        {
            var repositoryRoot = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", ".."));
            var migration = File.ReadAllText(Path.Combine(repositoryRoot, "database-scripts", "migrations", "20261017_AllowHostManagedStaffCategories.sql"));
            migration = migration.Replace("BEGIN;", string.Empty).Replace("COMMIT;", string.Empty);
            await using (var apply = new NpgsqlCommand(migration, connection, transaction))
            {
                await apply.ExecuteNonQueryAsync();
            }

            const string validSql = """
                INSERT INTO skolenza."StaffCategory"
                    ("Code", "Name", "DisplayOrder", "IsActive", "IsSystemDefined", "AddedDateTime")
                VALUES (@code, 'Host category rollback probe', 100, true, false, now())
                RETURNING "Id";
                """;
            short categoryId;
            await using (var insert = new NpgsqlCommand(validSql, connection, transaction))
            {
                insert.Parameters.AddWithValue("code", "PROBE_" + Guid.NewGuid().ToString("N"));
                categoryId = Convert.ToInt16(await insert.ExecuteScalarAsync());
            }
            Assert.That(categoryId, Is.GreaterThan(5));

            await using (var update = new NpgsqlCommand("""
                UPDATE skolenza."StaffMember" SET "StaffCategoryId" = @category
                WHERE "Id" = (SELECT MIN("Id") FROM skolenza."StaffMember");
                """, connection, transaction))
            {
                update.Parameters.AddWithValue("category", categoryId);
                Assert.That(await update.ExecuteNonQueryAsync(), Is.EqualTo(1));
            }

            await transaction.SaveAsync("invalid_category");
            await using var invalid = new NpgsqlCommand("""
                UPDATE skolenza."StaffMember" SET "StaffCategoryId" = 32767
                WHERE "Id" = (SELECT MIN("Id") FROM skolenza."StaffMember");
                """, connection, transaction);
            var exception = Assert.ThrowsAsync<PostgresException>(async () => await invalid.ExecuteNonQueryAsync());
            Assert.That(exception!.SqlState, Is.EqualTo(PostgresErrorCodes.ForeignKeyViolation));
            await transaction.RollbackAsync("invalid_category");
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }

    [Test]
    public void Proposed_category_ids_remain_stable()
    {
        Assert.Multiple(() =>
        {
            Assert.That(AppConstants.StaffCategories.Administrative, Is.EqualTo(1));
            Assert.That(AppConstants.StaffCategories.Teaching, Is.EqualTo(2));
            Assert.That(AppConstants.StaffCategories.NonTeaching, Is.EqualTo(3));
            Assert.That(AppConstants.StaffCategories.Support, Is.EqualTo(4));
            Assert.That(AppConstants.StaffCategories.Specialist, Is.EqualTo(5));
        });
    }

    [Test]
    public void Options_endpoint_is_authenticated_get_without_permission_identifiers()
    {
        var action = typeof(StaffCategoryController).GetMethod(nameof(StaffCategoryController.GetOptions));

        Assert.Multiple(() =>
        {
            Assert.That(action, Is.Not.Null);
            Assert.That(typeof(StaffCategoryController).GetCustomAttribute<AuthorizeAttribute>(), Is.Null);
            Assert.That(action!.GetCustomAttribute<AuthorizeAttribute>(), Is.Not.Null);
            Assert.That(action.GetCustomAttribute<HttpMethodAttribute>()?.HttpMethods, Does.Contain("GET"));
            Assert.That(action.GetParameters().Select(parameter => parameter.Name),
                Does.Not.Contain("moduleId").And.Not.Contain("operationId"));
        });
    }

    [Test]
    public void Migration_targets_staffMember_and_contains_the_complete_mapping()
    {
        var root = TestContext.CurrentContext.TestDirectory;
        var repositoryRoot = Path.GetFullPath(Path.Combine(root, "..", "..", "..", ".."));
        var script = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "database-scripts",
            "migrations",
            "20261011_AddStaffCategory.sql"));

        Assert.Multiple(() =>
        {
            Assert.That(script, Does.Contain("skolenza.\"StaffMember\""));
            Assert.That(script, Does.Not.Contain("skolenza.\"Employee\""));
            Assert.That(script, Does.Contain("'ADMINISTRATIVE'"));
            Assert.That(script, Does.Contain("'SPECIALIST'"));
            Assert.That(script, Does.Contain("WHEN 3 THEN 5"));
            Assert.That(script, Does.Contain("FK_StaffMember_StaffCategory"));
        });
    }

    [Test]
    public void StaffMember_create_detail_and_list_projections_return_the_persisted_category()
    {
        var root = TestContext.CurrentContext.TestDirectory;
        var repositoryRoot = Path.GetFullPath(Path.Combine(root, "..", "..", "..", ".."));
        var source = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "Skolenza.persistance",
            "Repositories",
            "BaseStaffMemberRepository.cs"));

        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Contain("StaffCategoryId = x.StaffMember.StaffCategoryId"));
            Assert.That(source, Does.Contain("StaffCategoryId = x.emp.StaffCategoryId"));
            Assert.That(
                source.Split("StaffCategoryId = emp.StaffCategoryId").Length - 1,
                Is.GreaterThanOrEqualTo(2),
                "Both staff creation response projections must return the persisted category.");
        });
    }

    [Test]
    public void Host_catalogue_endpoints_and_seed_use_the_persisted_permission_pipeline()
    {
        var root = TestContext.CurrentContext.TestDirectory;
        var repositoryRoot = Path.GetFullPath(Path.Combine(root, "..", "..", "..", ".."));
        var controller = File.ReadAllText(Path.Combine(repositoryRoot, "Skolenza.api", "Controllers", "Staff", "StaffCategoryController.cs"));
        var handler = File.ReadAllText(Path.Combine(repositoryRoot, "Skolenza.application", "Features", "HostStaffCategoryCmd", "Handlers", "HostStaffCategoryHandlers.cs"));
        var seed = File.ReadAllText(Path.Combine(repositoryRoot, "database-scripts", "SeedHostInstitutionMasterData.sql"));

        Assert.Multiple(() =>
        {
            Assert.That(controller, Does.Contain("HttpGet(\"host\")"));
            Assert.That(controller, Does.Contain("HttpPost(\"host\")"));
            Assert.That(controller, Does.Contain("HttpPut(\"host/{id}\")"));
            Assert.That(handler, Does.Contain("HostRuntimePermissionValidator.ValidateAsync"));
            Assert.That(handler, Does.Contain("Features.HostStaffCategoryCmd.Handlers"));
            Assert.That(seed, Does.Contain("'HOST_INSTITUTION_MASTER_DATA'"));
            Assert.That(seed, Does.Contain("'HOST_STAFF_CATEGORY_CATALOGUE'"));
            Assert.That(seed, Does.Contain("\"ModuleScope\"=2"));
            Assert.That(seed, Does.Contain("HostRoleModuleAndPermission"));
        });
    }
}
