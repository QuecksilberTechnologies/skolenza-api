using System.Text.RegularExpressions;
using skolenza.application.DTOS.Location;
using skolenza.persistance.Data.Context;
using skolenza.persistance.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("GlobalStateSeed")]
public sealed class GlobalStateSeedTests
{
    private static string Seed()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Skolenza.sln")))
        {
            directory = directory.Parent;
        }
        return File.ReadAllText(Path.Combine(directory!.FullName, "database-scripts", "SeedGlobalStates.sql"));
    }

    [Test]
    public void Global_state_seed_is_attributed_additive_and_preserves_detailed_catalogues()
    {
        var sql = Seed();
        Assert.Multiple(() =>
        {
            Assert.That(sql, Does.Contain("GeoNames CC BY 4.0"));
            Assert.That(sql, Does.Contain("3865 ADM1 rows across 228 country codes"));
            Assert.That(sql, Does.Contain("NOT IN ('IN', 'CN', 'DE', 'US', 'AE')"));
            Assert.That(sql, Does.Contain("NOT EXISTS"));
            Assert.That(sql, Does.Contain("pg_advisory_xact_lock"));
            Assert.That(sql, Does.Not.Contain("UPDATE skolenza."));
            Assert.That(sql, Does.Not.Contain("DELETE FROM"));
            Assert.That(sql, Does.Not.Contain("INSERT INTO skolenza.\"District\""));
            Assert.That(sql, Does.Not.Contain("INSERT INTO skolenza.\"Locality\""));
        });
    }

    [Test]
    public async Task Global_state_source_rows_resolve_and_Austria_Australia_options_are_available()
    {
        var path = Environment.GetEnvironmentVariable("SKOLENZA_CONTACT_DB_SETTINGS");
        if (string.IsNullOrWhiteSpace(path))
        {
            Assert.Ignore("Opt-in approved Development DB; global State seed required.");
        }
        var config = new ConfigurationBuilder().AddJsonFile(path!).Build();
        await using var db = new SkolenzaDbContext(new DbContextOptionsBuilder<SkolenzaDbContext>()
            .UseNpgsql(config.GetConnectionString("DefaultConnection")).Options);
        var countries = await db.Countries.AsNoTracking().Where(country => country.IsActive == true)
            .ToDictionaryAsync(country => country.CountryCode!, country => country.Id);
        var states = await db.States.AsNoTracking().ToListAsync();
        var source = Regex.Matches(Seed(),
            @"^\s*\('([A-Z]{2})', '([^']+)', '((?:''|[^'])*)', '((?:''|[^'])*)', '[0-9]+'\)[,;]?\r?$",
            RegexOptions.Multiline);
        Assert.That(source.Count, Is.EqualTo(3865));
        var preserved = new HashSet<string> { "IN", "CN", "DE", "US", "AE" };
        foreach (Match row in source)
        {
            var country = row.Groups[1].Value;
            if (preserved.Contains(country) || !countries.TryGetValue(country, out var countryId))
            {
                continue;
            }
            var code = row.Groups[2].Value;
            var name = row.Groups[3].Value.Replace("''", "'");
            var asciiName = row.Groups[4].Value.Replace("''", "'");
            Assert.That(states.Any(state => state.CountryId == countryId && state.IsActive == true &&
                (state.StateCode == code || string.Equals(state.StateName, name, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(state.StateName, asciiName, StringComparison.OrdinalIgnoreCase))), Is.True, country + "." + code);
        }
        var repository = new LocationRepository(db, NullLogger<LocationRepository>.Instance);
        var austria = await repository.GetStateOptionAsync(new GetStateOptionRequestDTO { CountryId = countries["AT"] });
        var australia = await repository.GetStateOptionAsync(new GetStateOptionRequestDTO { CountryId = countries["AU"] });
        Assert.Multiple(() =>
        {
            Assert.That(austria, Has.Count.EqualTo(9));
            Assert.That(austria.Any(state => state.StateName == "Vienna"), Is.True);
            Assert.That(australia, Has.Count.EqualTo(8));
            Assert.That(australia.Any(state => state.StateName == "New South Wales"), Is.True);
            Assert.That(austria.All(state => state.CountryCode == "AT"), Is.True);
            Assert.That(australia.All(state => state.CountryCode == "AU"), Is.True);
            Assert.That(states.Where(state => state.IsActive == true).Select(state => state.CountryId).Distinct().Count(), Is.EqualTo(227));
        });
    }
}
