using skolenza.api.Controllers.Location;
using skolenza.application.Constants;
using skolenza.application.DTOS.Location;
using skolenza.application.DTOS.InstitutionConfiguration;
using skolenza.domain.Entity;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("LocalityRefactor")]
public sealed class LocalityRefactorTests
{
    [Test]
    public void Locality_model_requires_district_and_type_relationships()
    {
        Assert.Multiple(() =>
        {
            Assert.That(typeof(Locality).GetProperty(nameof(Locality.DistrictId)), Is.Not.Null);
            Assert.That(typeof(Locality).GetProperty(nameof(Locality.LocalityTypeId)), Is.Not.Null);
            Assert.That(typeof(Locality).GetProperty(nameof(Locality.LocalityName)), Is.Not.Null);
            Assert.That(typeof(LocalityType).GetProperty(nameof(LocalityType.TypeName)), Is.Not.Null);
        });
    }

    [Test]
    public void Locality_type_constants_are_stable_and_match_seed_contract()
    {
        Assert.Multiple(() =>
        {
            Assert.That(LocalityTypeConstants.Values[LocalityTypeConstants.CityId], Is.EqualTo("City"));
            Assert.That(LocalityTypeConstants.Values[LocalityTypeConstants.TownId], Is.EqualTo("Town"));
            Assert.That(LocalityTypeConstants.Values[LocalityTypeConstants.VillageId], Is.EqualTo("Village"));
            Assert.That(LocalityTypeConstants.Values[LocalityTypeConstants.OtherId], Is.EqualTo("Other / Unclassified"));
            Assert.That(LocalityTypeConstants.Values, Has.Count.EqualTo(4));
        });
    }

    [Test]
    public void Locality_option_contract_requires_district_and_returns_type_context()
    {
        Assert.Multiple(() =>
        {
            Assert.That(typeof(GetLocalityOptionRequestDTO).GetProperty(nameof(GetLocalityOptionRequestDTO.DistrictId)), Is.Not.Null);
            Assert.That(typeof(GetLocalityOptionResponseDTO).GetProperty(nameof(GetLocalityOptionResponseDTO.LocalityTypeId)), Is.Not.Null);
            Assert.That(typeof(GetLocalityOptionResponseDTO).GetProperty(nameof(GetLocalityOptionResponseDTO.LocalityTypeName)), Is.Not.Null);
        });
    }

    [Test]
    public void Institution_location_contract_uses_district_and_locality_names()
    {
        var createProperties = typeof(CreateCampusRequestDTO).GetProperties().Select(x => x.Name).ToArray();
        var responseProperties = typeof(CampusResponseDTO).GetProperties().Select(x => x.Name).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(createProperties, Does.Contain("DistrictId"));
            Assert.That(createProperties, Does.Contain("LocalityId"));
            Assert.That(createProperties, Does.Not.Contain("CityId"));
            Assert.That(responseProperties, Does.Contain("DistrictName"));
            Assert.That(responseProperties, Does.Contain("LocalityName"));
            Assert.That(responseProperties, Does.Not.Contain("CityName"));
        });
    }

    [Test]
    public void Location_master_contract_exposes_codes_and_keeps_postal_code_on_locality()
    {
        Assert.Multiple(() =>
        {
            Assert.That(typeof(State).GetProperty(nameof(State.StateCode)), Is.Not.Null);
            Assert.That(typeof(District).GetProperty(nameof(District.DistrictCode)), Is.Not.Null);
            Assert.That(typeof(District).GetProperty("PinCode"), Is.Null);
            Assert.That(typeof(Locality).GetProperty(nameof(Locality.LocalityCode)), Is.Not.Null);
            Assert.That(typeof(Locality).GetProperty(nameof(Locality.PostalCode)), Is.Not.Null);
        });
    }

    [Test]
    public void Code_and_postal_migration_removes_district_pin_code()
    {
        var sql = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "database-scripts",
            "AddLocationCodesAndPostalCode.sql"));

        Assert.Multiple(() =>
        {
            Assert.That(sql, Does.Contain("DROP COLUMN IF EXISTS \"PinCode\""));
            Assert.That(sql, Does.Contain("ADD COLUMN IF NOT EXISTS \"StateCode\""));
            Assert.That(sql, Does.Contain("ADD COLUMN IF NOT EXISTS \"LocalityCode\""));
            Assert.That(sql, Does.Contain("ADD COLUMN IF NOT EXISTS \"PostalCode\""));
            Assert.That(sql, Does.Contain("UX_Locality_DistrictId_LocalityCode"));
        });
    }

    [Test]
    public void Four_country_postal_seed_is_idempotent_and_populates_locality_postal_code()
    {
        var sql = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "database-scripts",
            "SeedFourCountryPostalLocalities.sql"));

        Assert.Multiple(() =>
        {
            Assert.That(sql, Does.Contain("GeoNames postal-code exports"));
            Assert.That(sql, Does.Contain("('India','IN','+91',TRUE)"));
            Assert.That(sql, Does.Contain("('China','CN','+86',TRUE)"));
            Assert.That(sql, Does.Contain("('Germany','DE','+49',TRUE)"));
            Assert.That(sql, Does.Contain("('United States','US','+1',TRUE)"));
            Assert.That(sql, Does.Contain("\"PostalCode\"=EXCLUDED.\"PostalCode\""));
            Assert.That(sql, Does.Contain("ON CONFLICT (\"DistrictId\",\"LocalityCode\")"));
            Assert.That(sql, Does.Contain("(4, 'Other / Unclassified', TRUE)"));
        });
    }

    [TestCase(nameof(LocationController.GetLocality), "Locality/option")]
    [TestCase(nameof(LocationController.GetLocalityType), "LocalityType/option")]
    public void Location_lookup_routes_are_exposed(string actionName, string route)
    {
        var method = typeof(LocationController).GetMethod(actionName);
        var attribute = method?.GetCustomAttributes(typeof(HttpGetAttribute), false)
            .Cast<HttpGetAttribute>()
            .SingleOrDefault();

        Assert.That(attribute?.Template, Is.EqualTo(route));
    }

    [Test]
    public void Migration_is_data_preserving_guarded_and_seeds_all_locality_types()
    {
        var sql = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "database-scripts",
            "RenameCityToLocality.sql"));

        Assert.Multiple(() =>
        {
            Assert.That(sql, Does.Contain("ALTER TABLE skolenza.\"City\" RENAME TO \"Locality\""));
            Assert.That(sql, Does.Contain("district.\"Id\" = locality.\"Id\""));
            Assert.That(sql, Does.Contain("'Unassigned / Legacy'"));
            Assert.That(sql, Does.Contain("pg_get_serial_sequence('skolenza.\"District\"', 'Id')"));
            Assert.That(sql, Does.Contain("RAISE EXCEPTION '% Locality row(s) cannot be mapped"));
            Assert.That(sql, Does.Contain("(1, 'City', TRUE)"));
            Assert.That(sql, Does.Contain("(2, 'Town', TRUE)"));
            Assert.That(sql, Does.Contain("(3, 'Village', TRUE)"));
            Assert.That(sql, Does.Contain("(4, 'Other / Unclassified', TRUE)"));
            Assert.That(sql, Does.Contain("CREATE VIEW skolenza.\"City\""));
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
