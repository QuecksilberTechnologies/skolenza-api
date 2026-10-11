// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Guards runtime prerequisites required after the canonical production reset.
// ================================================================

using NUnit.Framework;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("CanonicalProductionSeed")]
public sealed class CanonicalProductionSeedTests
{
    [Test]
    public void Shared_master_seed_contains_the_global_permanent_onboarding_template()
    {
        var repositoryRoot = FindRepositoryRoot();
        var seed = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "database-scripts",
            "production-seed",
            "02-parent-master",
            "001-shared-master-data.sql"));
        var verification = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "database-scripts",
            "production-seed",
            "99-verification",
            "001-verify-canonical-seed.sql"));

        Assert.Multiple(() =>
        {
            Assert.That(seed, Does.Contain("INSERT INTO skolenza.\"StaffEmploymentType\""));
            Assert.That(seed, Does.Contain("(1, NULL, 'Permanent'"));
            Assert.That(verification, Does.Contain("Global Permanent staffMember onboarding template is missing."));
        });
    }

    [Test]
    public void Canonical_seed_contains_requested_employment_regulatory_and_plan_masters()
    {
        var repositoryRoot = FindRepositoryRoot();
        var countrySeed = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "database-scripts",
            "production-seed",
            "03-geography",
            "000-iso-country-catalog.sql"));
        var parentSeed = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "database-scripts",
            "production-seed",
            "02-parent-master",
            "001-shared-master-data.sql"));
        var regulatorySeed = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "database-scripts",
            "production-seed",
            "03-geography",
            "002-country-regulatory-master.sql"));
        var constants = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "skolenza.application",
            "Constants",
            "AppConstants.cs"));

        Assert.Multiple(() =>
        {
            Assert.That(countrySeed, Does.Contain("ISO country seed must contain exactly 249 rows."));
            Assert.That(countrySeed, Does.Contain("('India', 'IN', '+91')"));
            Assert.That(countrySeed, Does.Contain("('United States', 'US', '+1')"));
            Assert.That(parentSeed, Does.Contain("'Probationer'"));
            Assert.That(parentSeed, Does.Contain("'Intern'"));
            Assert.That(parentSeed, Does.Contain("INSERT INTO skolenza.\"SubscriptionPlan\""));
            Assert.That(regulatorySeed, Does.Contain("('IN', 'Provident Fund (PF)')"));
            Assert.That(regulatorySeed, Does.Contain("('CN', 'Basic Pension Insurance')"));
            Assert.That(regulatorySeed, Does.Contain("('DE', 'Statutory Pension Insurance')"));
            Assert.That(regulatorySeed, Does.Contain("('US', 'Social Security')"));
            Assert.That(regulatorySeed, Does.Contain("('Payroll and Tax Compliance')"));
            Assert.That(regulatorySeed, Does.Contain("<> 1018"));
            Assert.That(constants, Does.Contain("RoleTypeClient = 4"));
        });
    }

    [Test]
    public void StaffMember_identity_flow_persists_and_uses_country_document_configuration()
    {
        var repositoryRoot = FindRepositoryRoot();
        var identitySeed = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "database-scripts",
            "production-seed",
            "05-dependent-master",
            "001-staff-member-identity-catalog.sql"));
        var handler = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "skolenza.application",
            "Features",
            "StaffMemberCmd",
            "IdentitiesInfo",
            "Handlers",
            "CreateIdentityInfoCommandHandler.cs"));

        Assert.Multiple(() =>
        {
            Assert.That(identitySeed, Does.Contain("('PASSPORT', 'Passport'"));
            Assert.That(identitySeed, Does.Contain("('NINO', 'National Insurance Number'"));
            Assert.That(identitySeed, Does.Contain("('GB', 'GBR', 'United Kingdom', 'NINO')"));
            Assert.That(identitySeed, Does.Contain("('IN', 'IND', 'India', 'EPIC')"));
            Assert.That(identitySeed, Does.Contain("('IN', 'IND', 'India', 'UAN')"));
            Assert.That(identitySeed, Does.Contain("('US', 'USA', 'United States', 'ITIN')"));
            Assert.That(identitySeed, Does.Contain("('CA', 'CAN', 'Canada', 'SIN')"));
            Assert.That(identitySeed, Does.Contain("FROM skolenza.\"Country\" country"));
            Assert.That(handler, Does.Contain("await _unitOfWork.SaveChangesAsync(cancellationToken);"));
        });
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(
                current.FullName,
                "database-scripts",
                "production-seed",
                "02-parent-master",
                "001-shared-master-data.sql")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Repository root was not found from the test directory.");
    }
}
