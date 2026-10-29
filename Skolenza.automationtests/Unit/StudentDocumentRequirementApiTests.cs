using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using skolenza.application.Constants;
using NUnit.Framework;
using skolenza.api.Controllers.Student;
using skolenza.application.DTOS.Student;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("StudentDocumentRequirement")]
public sealed class StudentDocumentRequirementApiTests
{
    [Test]
    public void Persistence_model_maps_institution_master_and_location_session_scope()
    {
        using var context = new SkolenzaDbContext(
            new DbContextOptionsBuilder<SkolenzaDbContext>()
                .UseNpgsql("Host=localhost;Database=skolenza_model_validation;Username=postgres;Password=not-used")
                .Options);

        var entity = context.Model.FindEntityType(typeof(StudentDocumentRequirementMaster));

        Assert.Multiple(() =>
        {
            Assert.That(entity?.GetTableName(), Is.EqualTo("StudentDocumentRequirementMaster"));
            Assert.That(entity?.GetSchema(), Is.EqualTo("skolenza"));
            Assert.That(entity?.GetIndexes().Any(index => index.IsUnique), Is.True);
            Assert.That(context.Model.FindEntityType(typeof(StudentDocumentRequirementMaster))?.FindProperty(nameof(StudentDocumentRequirementMaster.CampusId)), Is.Not.Null);
            Assert.That(context.Model.FindEntityType(typeof(StudentDocumentRequirementMaster))?.FindProperty(nameof(StudentDocumentRequirementMaster.AcademicSessionId)), Is.Not.Null);
        });
    }

    [Test]
    public void Controller_is_authenticated_and_exposes_paged_master_mutations()
    {
        var controller = typeof(StudentDocumentRequirementController);
        var routes = controller.GetMethods().Select(method => method.Name).ToHashSet();

        Assert.Multiple(() =>
        {
            Assert.That(controller.GetCustomAttributes(typeof(AuthorizeAttribute), true), Is.Not.Empty);
            Assert.That(controller.GetCustomAttributes(typeof(RouteAttribute), true), Is.Not.Empty);
            Assert.That(routes, Does.Contain("GetPage"));
            Assert.That(routes, Does.Contain("Create"));
            Assert.That(routes, Does.Contain("Update"));
            Assert.That(routes, Does.Contain("Delete"));
        });
    }

    [Test]
    public void Document_requirement_filter_uses_central_pagination_defaults()
    {
        var filter = new StudentDocumentRequirementFilterDTO();

        Assert.Multiple(() =>
        {
            Assert.That(filter.PageNumber, Is.EqualTo(1));
            Assert.That(filter.PageSize, Is.EqualTo(10));
            Assert.That(filter.ActiveOnly, Is.True);
        });
    }

    [Test]
    public void Migration_seeds_dynamic_permission_operations_without_numeric_ids()
    {
        var root = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", ".."));
        var script = File.ReadAllText(Path.Combine(root, "database-scripts", "CreateStudentDocumentRequirementMasterAndModule.sql"));

        Assert.Multiple(() =>
        {
            Assert.That(script, Does.Contain(StudentConstants.AdmissionsModuleCode));
            Assert.That(script, Does.Contain("CROSS JOIN skolenza.\"Operation\""));
            Assert.That(script, Does.Contain("'view', 'add', 'update', 'delete'"));
            Assert.That(script, Does.Contain("InstitutionId"));
            Assert.That(script, Does.Contain("CampusId"));
            Assert.That(script, Does.Contain("AcademicSessionId"));
        });
    }
}
