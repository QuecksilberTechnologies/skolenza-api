using NUnit.Framework;

namespace skolenza.automationtests.Unit;

/// <summary>Guards the evidence-based removal of unused legacy database tables.</summary>
[TestFixture]
[Category("LegacyTableCleanup")]
public sealed class LegacyTableCleanupTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Test]
    public void Cleanup_script_is_transactional_guarded_and_restrictive()
    {
        var sql = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "database-scripts",
            "CleanupUnusedLegacyTables.sql"));

        Assert.Multiple(() =>
        {
            Assert.That(sql, Does.Contain("BEGIN;"));
            Assert.That(sql, Does.Contain("COMMIT;"));
            Assert.That(sql, Does.Contain("SELECT EXISTS"));
            Assert.That(sql, Does.Contain("contains data"));
            Assert.That(sql, Does.Contain("DROP TABLE skolenza.%I RESTRICT"));
        });
    }

    [TestCase("DistrictMaster")]
    [TestCase("HolidayMaster")]
    [TestCase("NoImagePath")]
    public void Removed_legacy_entity_has_no_runtime_reference(string entityName)
    {
        var runtimeRoots = new[]
        {
            "skolenza.api",
            "skolenza.application",
            "skolenza.domain",
            "skolenza.infrastructure",
            "skolenza.persistance"
        };

        var references = runtimeRoots
            .SelectMany(root => Directory.EnumerateFiles(
                Path.Combine(RepositoryRoot, root),
                "*.cs",
                SearchOption.AllDirectories))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Where(path => File.ReadAllText(path).Contains(entityName, StringComparison.Ordinal))
            .ToArray();

        Assert.That(references, Is.Empty);
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
