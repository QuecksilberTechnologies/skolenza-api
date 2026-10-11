using NUnit.Framework;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("DeletedTableModelQuarantine")]
public sealed class DeletedTableModelQuarantineTests
{
    private static readonly string[] RetiredEntityNames =
    {
        "BasicMenu", "Candidate", "CandidateCategorySkill",
        "StaffMemberDeviceAccessWindow", "ServiceProvider",
        "InstitutionStaffMemberSectionDefault", "TravelMode",
        "WorkflowStage", "WorkstationType"
    };

    [Test]
    public void Production_deleted_entities_are_explicitly_excluded_from_ef_model()
    {
        var root = FindRepositoryRoot();
        var context = File.ReadAllText(Path.Combine(
            root,
            "skolenza.persistance",
            "Data",
            "Context",
            "SkolenzaDbContext.cs"));

        Assert.Multiple(() =>
        {
            foreach (var entityName in RetiredEntityNames)
            {
                Assert.That(
                    context,
                    Does.Contain($"modelBuilder.Ignore<{entityName}>();"),
                    $"{entityName} must remain excluded while its production table is absent.");
            }
        });
    }

    [Test]
    public void Retired_policy_entities_are_hard_deleted_from_runtime_source()
    {
        var root = FindRepositoryRoot();
        var retiredEntities = new[]
        {
            "AccommodationAllowancePolicyByDesignation", "DayCombination",
            "StaffMemberLeaveBalance", "StaffMemberLeavePolicyMapping",
            "StaffMemberPolicyDependentMapping", "StaffMemberPolicyEnrollment",
            "InsurancePolicy", "InsurancePolicyDocument", "LeaveRule",
            "LeaveSandwichRule", "LeaveSandwichRuleMapping",
            "MealAllowancePolicyByDesignation", "PolicyLeaveTypeMapping",
            "PolicyTypeDocument", "PolicyTypeInsuranceMapping",
            "TravelAllowancePolicyByDesignation",
            "UnStructuredPolicyTypeMappingWithStaffEmploymentType"
        };
        var entityDirectory = Path.Combine(root, "skolenza.domain", "Entity");
        var context = File.ReadAllText(Path.Combine(
            root,
            "skolenza.persistance",
            "Data",
            "Context",
            "SkolenzaDbContext.cs"));

        Assert.Multiple(() =>
        {
            foreach (var entityName in retiredEntities)
            {
                Assert.That(
                    File.Exists(Path.Combine(entityDirectory, $"{entityName}.cs")),
                    Is.False,
                    $"{entityName} must not return to the runtime domain model.");
                Assert.That(
                    context,
                    Does.Not.Contain($"<{entityName}>")
                        .And.Not.Contain($"Entity<{entityName}>")
                        .And.Not.Contain($"Ignore<{entityName}>") ,
                    $"{entityName} must not remain configured or quarantined in EF.");
            }
        });
    }

    [Test]
    public void Retired_policy_cleanup_is_stable_code_scoped_and_seed_cannot_restore_module()
    {
        var root = FindRepositoryRoot();
        var cleanup = File.ReadAllText(Path.Combine(
            root,
            "database-scripts",
            "HardDeleteRetiredLegacyPolicyArtifacts.sql"));
        var moduleSeed = File.ReadAllText(Path.Combine(
            root,
            "database-scripts",
            "production-seed",
            "04-access-and-host",
            "001-modules-operations-two-host-admins.sql"));

        Assert.Multiple(() =>
        {
            Assert.That(cleanup, Does.Contain("\"ModuleCode\" = 'EMP_INSURANCE'"));
            Assert.That(cleanup, Does.Contain("DROP TABLE IF EXISTS"));
            Assert.That(cleanup, Does.Not.Contain("\"ModuleCode\" = 'INSTITUTION_POLICY"));
            Assert.That(moduleSeed, Does.Not.Contain("EMP_INSURANCE"));
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
