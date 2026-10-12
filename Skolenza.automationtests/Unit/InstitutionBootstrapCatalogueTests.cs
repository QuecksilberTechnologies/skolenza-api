using NUnit.Framework;
using skolenza.application.Common.SeedData;
using skolenza.application.Constants;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("InstitutionBootstrapCatalogue")]
public sealed class InstitutionBootstrapCatalogueTests
{
    [Test]
    public void Department_catalogue_contains_one_canonical_executive_office()
    {
        var definition = DepartmentConstants.DefaultDepartments.Single();

        Assert.Multiple(() =>
        {
            Assert.That(definition.Name, Is.EqualTo(DepartmentConstants.ExecutiveOfficeName));
            Assert.That(definition.IsExecutiveOffice, Is.True);
        });
    }

    [Test]
    public void Department_seed_helper_maps_the_authoritative_catalogue()
    {
        var department = DepartmentSeedHelper.GetRuntimeDepartmentsToSeed(11, 22).Single();

        Assert.Multiple(() =>
        {
            Assert.That(department.InstitutionId, Is.EqualTo(11));
            Assert.That(department.AddedById, Is.EqualTo(22));
            Assert.That(department.DepartmentName, Is.EqualTo(DepartmentConstants.ExecutiveOfficeName));
            Assert.That(department.IsExecutiveOffice, Is.True);
        });
    }

    [Test]
    public void Designation_seed_helper_maps_the_authoritative_catalogue_to_department_id()
    {
        var departmentMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            [DepartmentConstants.ExecutiveOfficeName] = 31
        };

        var designation = DesignationsSeedHelper
            .GetRuntimeDesignationsToSeed(11, 22, departmentMap)
            .Single();

        Assert.Multiple(() =>
        {
            Assert.That(designation.InstitutionId, Is.EqualTo(11));
            Assert.That(designation.AddedById, Is.EqualTo(22));
            Assert.That(designation.DepartmentId, Is.EqualTo(31));
            Assert.That(
                designation.DesignationName,
                Is.EqualTo(DesignationConstants.InstitutionAdministratorName));
        });
    }
}
