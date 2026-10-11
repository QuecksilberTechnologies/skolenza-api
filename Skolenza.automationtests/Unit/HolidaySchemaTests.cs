using skolenza.application.DTOs.Holiday;
using skolenza.domain.Entity;
using NUnit.Framework;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("HolidaySchema")]
public sealed class HolidaySchemaTests
{
    [Test]
    public void Entity_uses_location_and_date_without_redundant_geography_fields()
    {
        var properties = typeof(Holiday)
            .GetProperties()
            .Select(property => property.Name)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(properties, Does.Contain("CampusId"));
            Assert.That(properties, Does.Contain("Icon"));
            Assert.That(typeof(Holiday).GetProperty("HolidayDate")!.PropertyType, Is.EqualTo(typeof(DateOnly)));
            Assert.That(properties, Does.Not.Contain("CountryCode"));
            Assert.That(properties, Does.Not.Contain("StateCode"));
            Assert.That(properties, Does.Not.Contain("HolidayYear"));
            Assert.That(properties, Does.Not.Contain("Remark"));
        });
    }

    [Test]
    public void Response_contract_exposes_location_and_nonredundant_holiday_fields()
    {
        var properties = typeof(HolidayDTO)
            .GetProperties()
            .Select(property => property.Name)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(properties, Does.Contain("CampusId"));
            Assert.That(properties, Does.Contain("IsOptional"));
            Assert.That(properties, Does.Contain("Description"));
            Assert.That(properties, Does.Contain("Icon"));
            Assert.That(properties, Does.Not.Contain("StateCode"));
            Assert.That(properties, Does.Not.Contain("HolidayYear"));
        });
    }

    [Test]
    public void Rename_migration_preserves_table_and_renames_database_objects()
    {
        var root = FindRepositoryRoot();
        var sql = File.ReadAllText(Path.Combine(
            root,
            "database-scripts",
            "RenameOrganizationHolidayCalendarToHoliday.sql"));

        Assert.Multiple(() =>
        {
            Assert.That(sql, Does.Contain("RENAME TO \"Holiday\""));
            Assert.That(sql, Does.Contain("ADD COLUMN IF NOT EXISTS \"Icon\""));
            Assert.That(sql, Does.Contain("PK_Holiday"));
            Assert.That(sql, Does.Contain("FK_Holiday_Campus"));
            Assert.That(sql, Does.Contain("IX_Holiday_Location_Date"));
            Assert.That(sql, Does.Contain("UX_Holiday_Institution_Location_Date_NotDeleted"));
        });
    }

    [Test]
    public void Both_module_seeds_use_the_holiday_identity_and_operations()
    {
        var root = FindRepositoryRoot();
        var files = new[]
        {
            Path.Combine(root, "database-scripts", "complete seed data", "SeedInstitutionPolicyModules.sql"),
            Path.Combine(root, "database-scripts", "complete seed data", "Skolenza_New_Production_Module_Operation_Seed.sql")
        };

        Assert.Multiple(() =>
        {
            foreach (var file in files)
            {
                var sql = File.ReadAllText(file);
                Assert.That(sql, Does.Contain("'INSTITUTION_POLICY_HOLIDAY','Holiday','Holiday'"), file);
                Assert.That(sql, Does.Contain("('INSTITUTION_POLICY_HOLIDAY','Import',12,50)"), file);
                Assert.That(sql, Does.Contain("('INSTITUTION_POLICY_HOLIDAY','Export',11,60)"), file);
            }
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
