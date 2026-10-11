using System.Text.Json;
using NUnit.Framework;

namespace skolenza.automationtests.Unit;

[TestFixture]
public sealed class PolicyLiveEvidenceContractTests
{
    private JsonDocument _evidence = null!;
    private JsonDocument _namedPolicyEvidence = null!;

    [OneTimeSetUp]
    public void LoadEvidence()
    {
        var root = FindRepositoryRoot();
        var path = Path.Combine(
            root,
            "docs",
            "testing",
            "policy",
            "live-business-flow",
            "2026-09-16-api-evidence.json");

        Assert.That(File.Exists(path), Is.True, $"Live API evidence file was not found: {path}");
        _evidence = JsonDocument.Parse(File.ReadAllText(path));

        var namedPolicyPath = Path.Combine(
            root,
            "docs",
            "testing",
            "policy",
            "live-business-flow",
            "2026-09-16-named-policy-scenarios.json");

        Assert.That(File.Exists(namedPolicyPath), Is.True,
            $"Named policy evidence file was not found: {namedPolicyPath}");
        _namedPolicyEvidence = JsonDocument.Parse(File.ReadAllText(namedPolicyPath));
    }

    [OneTimeTearDown]
    public void DisposeEvidence()
    {
        _evidence.Dispose();
        _namedPolicyEvidence.Dispose();
    }

    [Test]
    public void Evidence_records_every_required_api_scenario_with_input_and_response()
    {
        var calls = _evidence.RootElement.GetProperty("calls").EnumerateArray().ToArray();
        var requiredCases = new[]
        {
            "P01", "P02", "P03", "P05", "P06", "P07", "P08",
            "P09", "P10", "P12", "P13", "P14", "P16"
        };

        Assert.Multiple(() =>
        {
            foreach (var requiredCase in requiredCases)
            {
                Assert.That(
                    calls.Any(call => call.GetProperty("case").GetString() == requiredCase),
                    Is.True,
                    $"Missing live evidence for {requiredCase}.");
            }

            foreach (var call in calls)
            {
                Assert.That(call.GetProperty("method").GetString(), Is.Not.Empty);
                Assert.That(call.GetProperty("endpoint").GetString(), Does.StartWith("/api/"));
                Assert.That(call.TryGetProperty("input", out _), Is.True);
                Assert.That(call.TryGetProperty("response", out _), Is.True);
            }
        });
    }

    [Test]
    public void Evidence_reconciles_persisted_policy_rows_and_keeps_blocked_upload_explicit()
    {
        var insertedData = _evidence.RootElement.GetProperty("insertedData").EnumerateArray().ToArray();
        var requiredTables = new[]
        {
            "skolenza.PolicyType",
            "skolenza.Policy",
            "skolenza.PolicyVersion",
            "skolenza.PolicyAssignment",
            "skolenza.PolicyException",
            "skolenza.PolicyAcknowledgement"
        };
        var upload = _evidence.RootElement.GetProperty("calls").EnumerateArray()
            .Single(call => call.GetProperty("case").GetString() == "P16");

        Assert.Multiple(() =>
        {
            foreach (var table in requiredTables)
            {
                Assert.That(
                    insertedData.Any(row => row.GetProperty("table").GetString() == table),
                    Is.True,
                    $"Missing DB reconciliation evidence for {table}.");
            }

            foreach (var row in insertedData)
            {
                Assert.That(row.GetProperty("id").GetInt64(), Is.GreaterThan(0));
                Assert.That(row.GetProperty("retained").GetBoolean(), Is.True);
            }

            Assert.That(upload.GetProperty("response").GetProperty("httpStatus").GetInt32(), Is.EqualTo(500));
            Assert.That(upload.GetProperty("response").GetProperty("metadataInserted").GetBoolean(), Is.False);
            Assert.That(upload.GetProperty("response").GetProperty("rootCause").GetString(), Does.Contain("AWS Access Key Id"));
        });
    }

    [Test]
    public void Evidence_contains_no_committed_authentication_secrets()
    {
        var raw = _evidence.RootElement.GetRawText();

        Assert.Multiple(() =>
        {
            Assert.That(raw, Does.Not.Contain("accessToken"));
            Assert.That(raw, Does.Not.Contain("refreshToken"));
            Assert.That(raw, Does.Not.Contain("password"));
            Assert.That(raw, Does.Not.Match("Bearer (?!<redacted>)[A-Za-z0-9_-]+"));
        });
    }

    [Test]
    public void Named_business_policies_are_inserted_updated_and_read_back_with_correct_rule_taxonomy()
    {
        var scenarios = _namedPolicyEvidence.RootElement.GetProperty("scenarios").EnumerateArray().ToArray();
        var expected = new Dictionary<string, (string Name, long[] RuleTypeIds)>
        {
            ["LEAVE"] = ("India Maharashtra Annual Leave Policy", new long[] { 2, 4, 5 }),
            ["ATTENDANCE"] = ("Hybrid Web Mobile Device Attendance Policy", new long[] { 7 }),
            ["INSURANCE"] = ("StaffMember Health Insurance Policy", new long[] { 1, 6 })
        };

        Assert.That(scenarios, Has.Length.EqualTo(expected.Count));

        Assert.Multiple(() =>
        {
            foreach (var scenario in scenarios)
            {
                var key = scenario.GetProperty("scenario").GetString()!;
                Assert.That(expected.ContainsKey(key), Is.True, $"Unexpected scenario {key}.");
                Assert.That(scenario.GetProperty("policyName").GetString(), Is.EqualTo(expected[key].Name));
                Assert.That(scenario.GetProperty("inserted").GetProperty("id").GetInt64(), Is.GreaterThan(0));
                Assert.That(scenario.GetProperty("inserted").GetProperty("versionId").GetInt64(), Is.GreaterThan(0));
                Assert.That(scenario.GetProperty("createResponse").GetProperty("isSucceeded").GetBoolean(), Is.True);
                Assert.That(scenario.GetProperty("updateResponse").GetProperty("isSucceeded").GetBoolean(), Is.True);
                Assert.That(scenario.GetProperty("correctedReadBackResponse").GetProperty("isSucceeded").GetBoolean(), Is.True);

                var ruleTypeIds = scenario.GetProperty("updateRequest").GetProperty("rules")
                    .EnumerateArray()
                    .Select(rule => rule.GetProperty("policyRuleTypeId").GetInt64())
                    .ToArray();
                Assert.That(ruleTypeIds, Is.EqualTo(expected[key].RuleTypeIds));
            }
        });
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "skolenza.sln")) ||
                Directory.Exists(Path.Combine(current.FullName, ".git")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Repository root could not be resolved from the test directory.");
    }
}
