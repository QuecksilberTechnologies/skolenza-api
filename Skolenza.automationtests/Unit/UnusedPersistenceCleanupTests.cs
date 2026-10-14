using NUnit.Framework;
using System.Text.RegularExpressions;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("UnusedPersistenceCleanup")]
public sealed class UnusedPersistenceCleanupTests
{
    private static readonly string[] RetiredEntities =
    [
        "ApprovalWorkflow", "WorkflowStep",
        "AssetHistory", "AssetTicketTypeDetail",
        "AttendanceHistory", "AttendanceLogs",
        "CandidateHistory", "DemoRequest", "DemoRequestBiometricDetail",
        "EmailsLog", "InterviewFeedback", "InterviewPanel",
        "InterviewPanelMember", "InterviewSchedule", "InterviewSdule",
        "LeaveTransactionLog", "TenderProject", "TenderService",
        "TenderServiceHistory", "TenderServiceProvider",
        "TenderServiceSpecification", "TenderServiceType"
    ];

    [Test]
    public void Retired_entities_have_no_runtime_source_reference()
    {
        var root = FindRepositoryRoot();
        var files = new[]
        {
            "skolenza.api", "skolenza.application", "skolenza.domain",
            "skolenza.infrastructure", "skolenza.persistance"
        }
        .SelectMany(folder => Directory.EnumerateFiles(
            Path.Combine(root, folder), "*.cs", SearchOption.AllDirectories))
        .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
        .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
        .ToArray();

        var references = RetiredEntities
            .SelectMany(entity => files
                .Where(path => Regex.IsMatch(
                    File.ReadAllText(path),
                    $@"\b{Regex.Escape(entity)}\b",
                    RegexOptions.CultureInvariant))
                .Select(path => $"{entity}: {path}"))
            .ToArray();

        Assert.That(references, Is.Empty);
    }

    [Test]
    public void Cleanup_script_guards_external_dependencies_and_uses_restrict()
    {
        var sql = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "database-scripts",
            "CleanupUnusedApiPersistencePhase2.sql"));

        Assert.Multiple(() =>
        {
            Assert.That(sql, Does.Contain("retained-table dependencies"));
            Assert.That(sql, Does.Contain("user triggers"));
            Assert.That(sql, Does.Contain("referenced by % views"));
            Assert.That(sql, Does.Contain("referenced by % routines"));
            Assert.That(sql, Does.Contain("DROP TABLE "));
            Assert.That(sql, Does.Contain(" RESTRICT"));
            Assert.That(sql, Does.Not.Contain("CASCADE"));
        });
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null &&
               !File.Exists(Path.Combine(directory.FullName, "Skolenza.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate Skolenza.sln.");
    }
}
