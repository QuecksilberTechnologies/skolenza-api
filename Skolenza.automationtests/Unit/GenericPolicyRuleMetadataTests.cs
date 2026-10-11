using System.Text.RegularExpressions;
using skolenza.application.Common.Helpers;
using NUnit.Framework;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("GenericPolicyRuleMetadata")]
public sealed class GenericPolicyRuleMetadataTests
{
    private static readonly string Root = FindRoot();

    [Test]
    public void Schema_creates_the_four_code_driven_rule_metadata_tables()
    {
        var sql = Read("database-scripts", "AddGenericPolicyRuleMetadata.sql");
        foreach (var table in new[]
        {
            "PolicyCategoryRuleType", "PolicyRuleSettingDefinition",
            "PolicyRuleSettingOption", "PolicyRuleSettingDependency"
        })
        {
            Assert.That(sql, Does.Contain($"CREATE TABLE IF NOT EXISTS skolenza.\"{table}\""));
        }
    }

    [Test]
    public void Seed_resolves_categories_rules_settings_and_options_by_stable_codes()
    {
        var sql = Read("database-scripts", "AddGenericPolicyRuleMetadata.sql");
        Assert.That(sql, Does.Contain("category.\"CategoryCode\" = mapping.\"CategoryCode\""));
        Assert.That(sql, Does.Contain("rule_type.\"RuleTypeCode\"=setting.\"RuleTypeCode\""));
        Assert.That(sql, Does.Contain("definition.\"SettingCode\"=option_seed.\"SettingCode\""));
        Assert.That(new Regex("PolicyCategoryId\\\"[^\\n]*VALUES\\s*\\([^)]*\\d", RegexOptions.IgnoreCase).IsMatch(sql), Is.False);
    }

    [Test]
    public void Seed_publishes_category_scoped_rule_mappings_and_user_facing_placeholders()
    {
        var sql = Read("database-scripts", "AddGenericPolicyRuleMetadata.sql");

        foreach (var mapping in new[]
        {
            "('INSURANCE','ELIGIBILITY'", "('INSURANCE','ENTITLEMENT'", "('INSURANCE','LIMIT'",
            "('ATTENDANCE','ATTENDANCE_CHANNEL'",
            "('LEAVE','ACCRUAL'", "('ATTENDANCE','LATE_PENALTY'", "('EXPENSE','REIMBURSEMENT'",
            "('CUSTOM','CUSTOM'"
        })
        {
            Assert.That(sql, Does.Contain(mapping), $"Missing category/rule mapping: {mapping}");
        }

        foreach (var placeholder in new[]
        {
            "'e.g. 180'", "'e.g. 12'", "'e.g. DAY'", "'e.g. MONTHLY'",
            "'true or false'", "'e.g. WEB'", "'e.g. INR'", "'e.g. INDIA_PUBLIC_HOLIDAYS'",
            "'e.g. CUSTOM_RULE_CODE'"
        })
        {
            Assert.That(sql, Does.Contain(placeholder), $"Missing setting placeholder: {placeholder}");
        }

        Assert.That(sql, Does.Contain("\"Placeholder\"=EXCLUDED.\"Placeholder\""));
        Assert.That(sql, Does.Contain("\"AllowMultiple\" boolean NOT NULL DEFAULT false"));
        Assert.That(sql, Does.Contain("ATTENDANCE_CHANNEL"));
        Assert.That(sql, Does.Contain("mapping.\"CategoryCode\" = 'LEAVE'"));
        Assert.That(sql, Does.Contain("\"AllowMultiple\"=EXCLUDED.\"AllowMultiple\""));
    }

    [Test]
    public void Lookup_contract_publishes_rule_definitions_for_dynamic_UI_fields()
    {
        var handler = Read("skolenza.application", "Features", "GenericPolicyCmd", "GenericPolicyHandlers.cs");
        var repository = Read("skolenza.persistance", "Repositories", "GenericPolicyRepository.cs");
        Assert.That(handler, Does.Contain("ruleDefinitions = await Repository.GetRuleDefinitionsAsync(token)"));
        Assert.That(repository, Does.Contain("PolicyRuleDefinitionResponseDTO"));
        Assert.That(repository, Does.Contain("PolicyRuleSettingOptionResponseDTO"));
        Assert.That(repository, Does.Contain("PolicyRuleSettingDependencyResponseDTO"));
    }

    [Test]
    public void Policy_type_contract_publishes_stable_category_code_for_rule_filtering()
    {
        var dto = Read("skolenza.application", "DTOS", "Policy", "GenericPolicyDTOs.cs");
        var repository = Read("skolenza.persistance", "Repositories", "GenericPolicyRepository.cs");

        Assert.That(dto, Does.Contain("int? CategoryId, string CategoryCode"));
        Assert.That(repository, Does.Contain("category.CategoryCode"));
        Assert.That(repository, Does.Contain("MapType(entity, categoryCode)"));
    }

    [Test]
    public void Save_validation_rejects_disallowed_unknown_wrong_type_and_out_of_range_settings()
    {
        var repository = Read("skolenza.persistance", "Repositories", "GenericPolicyRepository.cs");
        Assert.That(repository, Does.Contain("not allowed for this policy category"));
        Assert.That(repository, Does.Contain("is not supported"));
        Assert.That(repository, Does.Contain("must be {definition.DataTypeCode}"));
        Assert.That(repository, Does.Contain("is outside its allowed range"));
        Assert.That(repository, Does.Contain("has an unsupported option"));
        Assert.That(repository, Does.Contain("must be an array"));
    }

    [Test]
    public void Existing_assignment_and_unassignment_contract_remains_present()
    {
        var controller = Read("skolenza.api", "Controllers", "Policies", "InstitutionPolicyController.cs");
        Assert.That(controller, Does.Contain("[HttpPost(\"assignments\")]"));
        Assert.That(controller, Does.Contain("[HttpDelete(\"assignments/{assignmentId:long}\")]"));
    }

    [TestCase(12, "MONTHLY", 1)]
    [TestCase(18, "MONTHLY", 1.5)]
    [TestCase(20, "MONTHLY", 1.666667)]
    [TestCase(20, "QUARTERLY", 5)]
    [TestCase(20, "YEARLY", 20)]
    public void Accrual_amount_is_derived_from_the_authoritative_annual_entitlement(
        decimal annualEntitlement,
        string frequencyCode,
        decimal expectedAmount)
    {
        Assert.That(
            PolicyAccrualCalculationHelper.CalculateAmountPerCycle(annualEntitlement, frequencyCode),
            Is.EqualTo(expectedAmount));
    }

    [Test]
    public void Accrual_metadata_requires_annual_entitlement_before_the_derived_cycle_amount()
    {
        var sql = Read("database-scripts", "AddGenericPolicyRuleMetadata.sql");

        Assert.Multiple(() =>
        {
            Assert.That(sql, Does.Contain("('ACCRUAL','annualEntitlement','Annual entitlement','DECIMAL',true"));
            Assert.That(sql.IndexOf("'annualEntitlement'", StringComparison.Ordinal),
                Is.LessThan(sql.IndexOf("'amountPerCycle'", StringComparison.Ordinal)));
            Assert.That(sql, Does.Contain("The final cycle uses the remaining balance"));
        });
    }

    [Test]
    public void Save_validation_recomputes_and_rejects_a_conflicting_accrual_amount()
    {
        var repository = Read("skolenza.persistance", "Repositories", "GenericPolicyRepository.cs");

        Assert.Multiple(() =>
        {
            Assert.That(repository, Does.Contain("ValidateAccrualCalculation(ruleDefinitions, root)"));
            Assert.That(repository, Does.Contain("PolicyAccrualCalculationHelper.CalculateAmountPerCycle"));
            Assert.That(repository, Does.Contain("must equal {expectedAmount}"));
        });
    }

    private static string Read(params string[] parts) => File.ReadAllText(Path.Combine(new[] { Root }.Concat(parts).ToArray()));

    private static string FindRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null && !File.Exists(Path.Combine(current.FullName, "Skolenza.sln")))
        {
            current = current.Parent;
        }
        return current?.FullName ?? throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
