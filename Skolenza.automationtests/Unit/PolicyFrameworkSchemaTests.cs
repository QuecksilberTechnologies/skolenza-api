using skolenza.application.Common.Enums;
using NUnit.Framework;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("PolicyFramework")]
public sealed class PolicyFrameworkSchemaTests
{
    [Test]
    public void Schema_contains_versioned_policy_governance_tables_and_json_rules()
    {
        var sql = ReadRepositoryFile("database-scripts", "CreateGenericInstitutionPolicyFramework.sql");
        var tables = new[]
        {
            "PolicyCategory", "PolicyStatus", "PolicyRuleType", "PolicyDocumentType",
            "PolicyType", "Policy", "PolicyVersion", "PolicyRule", "PolicyApplicability",
            "PolicyAssignment", "PolicyException", "PolicyDocument", "PolicyApprovalStage",
            "PolicyApprovalHistory", "PolicyAcknowledgement", "PolicyChangeAudit"
        };

        Assert.Multiple(() =>
        {
            foreach (var table in tables)
            {
                Assert.That(sql, Does.Contain($"CREATE TABLE IF NOT EXISTS skolenza.\"{table}\""), table);
            }

            Assert.That(sql, Does.Contain("\"RuleConfiguration\" jsonb NOT NULL"));
            Assert.That(sql, Does.Contain("UX_PolicyVersion_Current"));
            Assert.That(sql, Does.Contain("CK_PolicyVersion_EffectiveDates"));
            Assert.That(sql, Does.Contain("FK_PolicyApplicability_Campus"));
            Assert.That(sql, Does.Contain("FK_PolicyApplicability_StaffEmploymentType"));
        });
    }

    [Test]
    public void Module_seed_has_one_parent_seven_leaf_modules_and_stable_page_names()
    {
        var sql = ReadRepositoryFile("database-scripts", "complete seed data", "SeedInstitutionPolicyModules.sql");
        var leafCodes = new[]
        {
            "INSTITUTION_POLICY_TYPES", "INSTITUTION_POLICY_DEFINITIONS", "INSTITUTION_POLICY_ASSIGNMENTS",
            "INSTITUTION_POLICY_EXCEPTIONS", "INSTITUTION_POLICY_APPROVALS",
            "INSTITUTION_POLICY_ACKNOWLEDGEMENTS", "INSTITUTION_POLICY_AUDIT"
        };

        Assert.Multiple(() =>
        {
            Assert.That(sql, Does.Contain("('INSTITUTION_POLICIES','Institution-Policies','Policies',NULL,'institution-policies-root',NULL,false"));
            foreach (var code in leafCodes)
            {
                Assert.That(sql, Does.Contain($"('{code}'"), code);
            }

            Assert.That(sql, Does.Contain("\"ModuleScope\"=1"));
            Assert.That(sql, Does.Contain("ADD COLUMN IF NOT EXISTS \"PageName\""));
            Assert.That(sql, Does.Contain("\"PageName\"=COALESCE(module.\"PageName\",seed.\"PageName\")"));
            Assert.That(sql, Does.Contain("INSERT INTO skolenza.\"PlanModuleMapping\""));
        });
    }

    [Test]
    public void Policy_operations_reuse_existing_names_and_add_only_missing_lifecycle_actions()
    {
        var sql = ReadRepositoryFile("database-scripts", "complete seed data", "SeedInstitutionPolicyModules.sql");

        Assert.Multiple(() =>
        {
            Assert.That((int)OperationType.Publish, Is.EqualTo(28));
            Assert.That((int)OperationType.Archive, Is.EqualTo(29));
            Assert.That((int)OperationType.Acknowledge, Is.EqualTo(30));
            Assert.That(sql, Does.Contain("WHERE NOT EXISTS (SELECT 1 FROM skolenza.\"Operation\" operation WHERE lower(btrim(operation.\"OperationName\"))=lower(seed.\"OperationName\"))"));
            Assert.That(sql, Does.Contain("('Publish',28"));
            Assert.That(sql, Does.Contain("('Archive',29"));
            Assert.That(sql, Does.Contain("('Acknowledge',30"));
            Assert.That(sql, Does.Contain("ORDER BY CASE WHEN candidate.\"OperationType\"=seed.\"OperationType\" THEN 0 ELSE 1 END"));
        });
    }

    [Test]
    public void Consolidated_production_seed_contains_policy_module_mirror()
    {
        var sql = ReadRepositoryFile(
            "database-scripts",
            "complete seed data",
            "Skolenza_New_Production_Module_Operation_Seed.sql");

        Assert.Multiple(() =>
        {
            Assert.That(sql, Does.Contain("SECTION: INSTITUTION POLICY FRAMEWORK MODULES (canonical mirror)"));
            Assert.That(sql, Does.Contain("INSTITUTION_POLICIES"));
            Assert.That(sql, Does.Contain("INSTITUTION_POLICY_ACKNOWLEDGEMENTS"));
            Assert.That(sql, Does.Not.Contain("\\i"));
        });
    }

    [Test]
    public void Policy_assignment_module_maps_the_existing_download_operation()
    {
        var expected = "('INSTITUTION_POLICY_ASSIGNMENTS','Download',13,60)";
        var focusedSeed = ReadRepositoryFile(
            "database-scripts",
            "complete seed data",
            "SeedInstitutionPolicyModules.sql");
        var productionSeed = ReadRepositoryFile(
            "database-scripts",
            "complete seed data",
            "Skolenza_New_Production_Module_Operation_Seed.sql");

        Assert.Multiple(() =>
        {
            Assert.That(focusedSeed, Does.Contain(expected));
            Assert.That(productionSeed, Does.Contain(expected));
        });
    }

    [Test]
    public void Publication_integrity_migration_is_idempotent_and_preserves_historical_approvals()
    {
        var migration = ReadRepositoryFile("database-scripts", "AddPolicyPublicationIntegrity.sql");
        var baseline = ReadRepositoryFile("database-scripts", "CreateGenericInstitutionPolicyFramework.sql");

        Assert.Multiple(() =>
        {
            Assert.That(migration, Does.Contain("ADD COLUMN IF NOT EXISTS \"ApprovedContentChecksumSha256\""));
            Assert.That(migration, Does.Contain("ADD COLUMN IF NOT EXISTS \"ContentChecksumSha256\""));
            Assert.That(migration, Does.Contain("repeat('0', 64)"));
            Assert.That(migration, Does.Contain("ALTER COLUMN \"ContentChecksumSha256\" SET NOT NULL"));
            Assert.That(baseline, Does.Contain("\"ApprovedContentChecksumSha256\" varchar(64)"));
            Assert.That(baseline, Does.Contain("\"ContentChecksumSha256\" varchar(64) NOT NULL"));
        });
    }

    [Test]
    public void Leave_type_targeting_migration_is_normalized_idempotent_and_seeded()
    {
        var sql = ReadRepositoryFile("database-scripts", "AddPolicyLeaveTypeTargeting.sql");
        Assert.Multiple(() =>
        {
            Assert.That(sql, Does.Contain("CREATE TABLE IF NOT EXISTS skolenza.\"PolicyVersionLeaveType\""));
            Assert.That(sql, Does.Contain("CREATE TABLE IF NOT EXISTS skolenza.\"PolicyRuleLeaveType\""));
            Assert.That(sql, Does.Contain("CREATE TABLE IF NOT EXISTS skolenza.\"PolicyApplicabilityLeaveType\""));
            Assert.That(sql, Does.Contain("ON DELETE CASCADE"));
            Assert.That(sql, Does.Contain("ON DELETE RESTRICT"));
            Assert.That(sql, Does.Contain("Casual Leave"));
            Assert.That(sql, Does.Contain("Earned Leave"));
            Assert.That(sql, Does.Contain("Maternity Leave"));
            Assert.That(sql, Does.Contain("Paternity Leave"));
            Assert.That(sql, Does.Contain("Menstrual Leave"));
            Assert.That(sql, Does.Contain("Bereavement Leave"));
            Assert.That(sql, Does.Contain("Birthday Leave"));
        });
    }

    private static string ReadRepositoryFile(params string[] parts)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Skolenza.sln")))
        {
            directory = directory.Parent;
        }

        var root = directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate Skolenza.sln.");
        return File.ReadAllText(Path.Combine(new[] { root }.Concat(parts).ToArray()));
    }
}
