using Npgsql;
using NUnit.Framework;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("ModuleOperationSeed")]
public sealed class ModuleOperationSeedContractTests
{
    [Test]
    public void Consolidated_seed_keeps_canonical_institution_dashboard_and_completes_module_metadata()
    {
        var repositoryRoot = FindRepositoryRoot();
        var authoritativeSeed = Path.Combine(
            repositoryRoot,
            "database-scripts",
            "complete seed data",
            "Skolenza_New_Production_Module_Operation_Seed.sql");
        var removedDuplicateSeed = Path.Combine(
            repositoryRoot,
            "database-scripts",
            "Skolenza_New_Production_Module_Operation_Seed.sql");

        var sql = File.ReadAllText(authoritativeSeed);

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(removedDuplicateSeed), Is.False);
            Assert.That(sql, Does.Contain("'INSTITUTION_DASHBOARD'"));
            Assert.That(sql, Does.Not.Contain("'HOST_DASHBOARD'"));
            Assert.That(sql, Does.Contain("$module_presentation_metadata_validation$"));
            Assert.That(sql, Does.Contain("NULLIF(BTRIM(\"ImageIconWeb\"), '') IS NULL"));
            Assert.That(sql, Does.Contain("NULLIF(BTRIM(\"ImageIconMobile\"), '') IS NULL"));
            Assert.That(sql, Does.Contain("NULLIF(BTRIM(\"Remark\"), '') IS NULL"));
            Assert.That(sql, Does.Contain("Expected one active Add and zero Create operations"));
        });
    }

    [Test]
    public void Consolidated_seed_groups_master_leaves_under_singular_duplicate_safe_parents()
    {
        var repositoryRoot = FindRepositoryRoot();
        var authoritativeSeed = Path.Combine(
            repositoryRoot,
            "database-scripts",
            "complete seed data",
            "Skolenza_New_Production_Module_Operation_Seed.sql");
        var standaloneSeed = Path.Combine(
            repositoryRoot,
            "database-scripts",
            "SeedBulkImportModules.sql");

        var sql = File.ReadAllText(authoritativeSeed);
        var standaloneSql = File.ReadAllText(standaloneSeed);

        Assert.Multiple(() =>
        {
            Assert.That(sql, Does.Contain("('INSTITUTION_DEPARTMENT','Institution-Department','Institution Department'"));
            Assert.That(sql, Does.Contain("('INSTITUTION_ROLE','Institution-Role','Institution Role'"));
            Assert.That(sql, Does.Contain("independent roots, not children of INSTITUTION_MGMT"));
            Assert.That(sql, Does.Contain("WHEN 'DEPARTMENT' THEN department_parent_id"));
            Assert.That(sql, Does.Contain("WHEN 'ROLE' THEN role_parent_id"));
            Assert.That(sql, Does.Contain("WHEN 'STAFF_MEMBER_TYPE' THEN staffMember_parent_id"));
            Assert.That(sql, Does.Contain("('INSTITUTION_DEPARTMENTS','DEPARTMENT')"));
            Assert.That(sql, Does.Contain("('INSTITUTION_ROLES_PERMISSIONS','ROLE')"));
            Assert.That(sql, Does.Contain("('INSTITUTION_STAFF_MEMBER_TYPES','STAFF_MEMBER_TYPE')"));
            Assert.That(sql, Does.Contain("'institution-departments'"));
            Assert.That(sql, Does.Not.Contain("'INSTITUTION_DESIGNATION'"));
            Assert.That(sql, Does.Not.Contain("'DESIGNATION'"));
            Assert.That(sql, Does.Contain("'institution-roles-permissions'"));
            Assert.That(sql, Does.Contain("Each singular parent inherits the plans of its own functional child."));
            Assert.That(sql, Does.Contain("Existing institutions on plans containing these modules receive the same"));
            Assert.That(sql, Does.Contain("WHERE \"OperationType\" = 11"));
            Assert.That(sql, Does.Not.Contain("canonical bulk Export operation"));
            Assert.That(sql, Does.Contain("Auto-assigned during Institution plan entitlement synchronization"));
            Assert.That(standaloneSql, Does.Contain("('INSTITUTION_DEPARTMENTS')"));
            Assert.That(standaloneSql, Does.Not.Contain("INSTITUTION_STAFF_DESIGNATIONS"));
            Assert.That(standaloneSql, Does.Contain("('INSTITUTION_ACCESS')"));
            Assert.That(standaloneSql, Does.Contain("('STAFF_EMPLOYMENT_TYPES')"));
            Assert.That(standaloneSql, Does.Contain("('STAFF_DIRECTORY')"));
        });
    }

    [Test]
    public void Skolenza_migration_enforces_parent_header_and_direct_child_only()
    {
        var repositoryRoot = FindRepositoryRoot();
        var migration = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "database-scripts",
            "migrations",
            "2026-10-11-enforce-two-level-module-hierarchy.sql"));
        var catalogue = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "database-scripts",
            "deployment-bootstrap",
            "06-institution-staff-access-catalogue.sql"));

        Assert.Multiple(() =>
        {
            Assert.That(migration, Does.Contain("ValidateModuleTwoLevelHierarchy"));
            Assert.That(migration, Does.Contain("TR_Module_TwoLevelHierarchy"));
            Assert.That(migration, Does.Contain("Module hierarchy supports only Parent headers and Child options."));
            Assert.That(catalogue, Does.Contain("('INSTITUTION_DEPARTMENTS','Institution Departments','Departments','/app/departments','INSTITUTION_MGMT',TRUE"));
            Assert.That(catalogue, Does.Not.Contain("INSTITUTION_STAFF_DESIGNATIONS"));
            Assert.That(catalogue, Does.Contain("('INSTITUTION_ACCESS','Institution Access','Roles & Permissions','/app/roles','INSTITUTION_MGMT',TRUE"));
            Assert.That(catalogue, Does.Not.Contain("('DEPARTMENT','Departments','Departments'"));
            Assert.That(catalogue, Does.Not.Contain("('STAFF_DESIGNATIONS','Staff Designations','Designations'"));
            Assert.That(catalogue, Does.Not.Contain("('ACCESS_ROLES','Access Roles','Roles & Permissions'"));
        });
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Skolenza.sln")) &&
                Directory.Exists(Path.Combine(directory.FullName, "database-scripts")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Repository root containing database-scripts was not found.");
    }
}

[TestFixture]
[Category("ModuleOperationSeedDatabase")]
[NonParallelizable]
public sealed class ModuleOperationSeedDatabaseTests
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
    public async Task Module_metadata_is_complete_and_create_alias_is_absent()
    {
        var incompleteModules = await ScalarAsync("""
            SELECT count(*)
            FROM skolenza."Module"
            WHERE nullif(btrim("ImageIconWeb"), '') IS NULL
               OR nullif(btrim("ImageIconMobile"), '') IS NULL
               OR nullif(btrim("Remark"), '') IS NULL;
            """);

        var createOperations = await ScalarAsync("""
            SELECT count(*)
            FROM skolenza."Operation"
            WHERE lower(btrim("OperationName")) = 'create';
            """);

        var addOperations = await ScalarAsync("""
            SELECT count(*)
            FROM skolenza."Operation"
            WHERE lower(btrim("OperationName")) = 'add'
              AND "OperationType" = 1
              AND "IsActive";
            """);

        Assert.Multiple(() =>
        {
            Assert.That(incompleteModules, Is.Zero);
            Assert.That(createOperations, Is.Zero);
            Assert.That(addOperations, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Singular_navigation_parents_have_expected_children_without_duplicate_catalogue_rows()
    {
        var validHierarchyRows = await ScalarAsync("""
            SELECT count(*)
            FROM (VALUES
                ('INSTITUTION_DEPARTMENT','DEPARTMENT','institution-departments'),
                ('INSTITUTION_ROLE','ROLE','institution-roles-permissions')
            ) expected(parent_code,child_code,child_page_name)
            JOIN skolenza."Module" parent
              ON parent."ModuleCode"=expected.parent_code
             AND parent."ModuleScope"=1
             AND parent."IsActive"
             AND parent."IsModuleDisplayInUI"
             AND NOT parent."IsLeafNode"
             AND parent."ParentModuleId" IS NULL
             AND parent."URLPath" IS NULL
            JOIN skolenza."Module" child
              ON child."ModuleCode"=expected.child_code
             AND child."ModuleScope"=1
             AND child."ParentModuleId"=parent."Id"
             AND child."PageName"=expected.child_page_name
             AND child."IsLeafNode";
            """);

        var duplicateModules = await ScalarAsync("""
            SELECT count(*) FROM (
                SELECT "ModuleCode","ModuleScope"
                FROM skolenza."Module"
                WHERE "ModuleCode" IN
                    ('INSTITUTION_DEPARTMENT','INSTITUTION_ROLE')
                GROUP BY "ModuleCode","ModuleScope"
                HAVING count(*) > 1
            ) duplicate;
            """);

        var parentOperationMappings = await ScalarAsync("""
            SELECT count(*)
            FROM skolenza."Module" module
            JOIN skolenza."ModuleOperationMapping" mapping
              ON mapping."ModuleId"=module."Id"
            WHERE module."ModuleCode" IN
                ('INSTITUTION_DEPARTMENT','INSTITUTION_ROLE');
            """);

        var duplicatePlanMappings = await ScalarAsync("""
            SELECT count(*) FROM (
                SELECT mapping."SubscriptionPlanId",mapping."ModuleId"
                FROM skolenza."PlanModuleMapping" mapping
                JOIN skolenza."Module" module ON module."Id"=mapping."ModuleId"
                WHERE module."ModuleCode" IN
                    ('INSTITUTION_DEPARTMENT','INSTITUTION_ROLE')
                GROUP BY mapping."SubscriptionPlanId",mapping."ModuleId"
                HAVING count(*) > 1
            ) duplicate;
            """);

        var requiredLeafOperationMappings = await ScalarAsync("""
            SELECT count(*)
            FROM skolenza."Module" module
            JOIN skolenza."ModuleOperationMapping" mapping
              ON mapping."ModuleId"=module."Id" AND mapping."IsActive"
            JOIN skolenza."Operation" operation
              ON operation."Id"=mapping."OperationId" AND operation."IsActive"
            WHERE module."ModuleCode" IN
                ('DEPARTMENT','ROLE','STAFF_MEMBER_TYPE')
              AND lower(btrim(operation."OperationName")) IN
                ('add','update','delete','view','import','export');
            """);

        var duplicateLeafOperationNames = await ScalarAsync("""
            SELECT count(*) FROM (
                SELECT module."Id",lower(btrim(operation."OperationName"))
                FROM skolenza."Module" module
                JOIN skolenza."ModuleOperationMapping" mapping
                  ON mapping."ModuleId"=module."Id" AND mapping."IsActive"
                JOIN skolenza."Operation" operation
                  ON operation."Id"=mapping."OperationId" AND operation."IsActive"
                WHERE module."ModuleCode" IN
                    ('DEPARTMENT','ROLE','STAFF_MEMBER_TYPE')
                  AND lower(btrim(operation."OperationName")) IN
                    ('add','update','delete','view','import','export')
                GROUP BY module."Id",lower(btrim(operation."OperationName"))
                HAVING count(*) > 1
            ) duplicate;
            """);

        var legacyLeafRows = await ScalarAsync("""
            SELECT count(*)
            FROM skolenza."Module"
            WHERE "ModuleCode" IN
                ('INSTITUTION_DEPARTMENTS','INSTITUTION_DESIGNATIONS','INSTITUTION_ROLES_PERMISSIONS',
                 'INSTITUTION_STAFF_MEMBER_TYPES');
            """);

        var validStaffEmploymentTypeHierarchy = await ScalarAsync("""
            SELECT count(*)
            FROM skolenza."Module" child
            JOIN skolenza."Module" parent ON parent."Id"=child."ParentModuleId"
            WHERE child."ModuleCode"='STAFF_MEMBER_TYPE'
              AND child."ModuleScope"=1
              AND child."IsLeafNode"
              AND child."PageName"='institution-staff-employment-types'
              AND parent."ModuleCode"='EMP_MGMT'
              AND parent."ModuleScope"=1;
            """);

        var missingParentPlanMappings = await ScalarAsync("""
            SELECT count(*)
            FROM (VALUES
                ('INSTITUTION_DEPARTMENT','DEPARTMENT'),
                ('INSTITUTION_ROLE','ROLE')
            ) expected(parent_code,child_code)
            JOIN skolenza."Module" parent ON parent."ModuleCode"=expected.parent_code
            JOIN skolenza."Module" child ON child."ModuleCode"=expected.child_code
            JOIN skolenza."PlanModuleMapping" child_plan
              ON child_plan."ModuleId"=child."Id" AND child_plan."IsActive"
            WHERE NOT EXISTS
            (
                SELECT 1
                FROM skolenza."PlanModuleMapping" parent_plan
                WHERE parent_plan."SubscriptionPlanId"=child_plan."SubscriptionPlanId"
                  AND parent_plan."ModuleId"=parent."Id"
                  AND parent_plan."IsActive"
            );
            """);

        Assert.Multiple(() =>
        {
            Assert.That(validHierarchyRows, Is.EqualTo(2));
            Assert.That(duplicateModules, Is.Zero);
            Assert.That(parentOperationMappings, Is.Zero);
            Assert.That(duplicatePlanMappings, Is.Zero);
            Assert.That(requiredLeafOperationMappings, Is.EqualTo(18));
            Assert.That(duplicateLeafOperationNames, Is.Zero);
            Assert.That(legacyLeafRows, Is.Zero);
            Assert.That(missingParentPlanMappings, Is.Zero);
            Assert.That(validStaffEmploymentTypeHierarchy, Is.EqualTo(1));
        });
    }

    private async Task<long> ScalarAsync(string sql)
    {
        await using var command = new NpgsqlCommand(sql, _connection);
        return Convert.ToInt64(await command.ExecuteScalarAsync());
    }
}
