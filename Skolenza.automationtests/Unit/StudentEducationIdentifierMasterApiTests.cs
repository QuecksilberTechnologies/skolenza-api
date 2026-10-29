using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using skolenza.api.Controllers.Student;
using skolenza.application.Constants;
using skolenza.application.DTOS.Student;
using skolenza.application.Features.StudentCmd;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("StudentEducationIdentifierMaster")]
public sealed class StudentEducationIdentifierMasterApiTests
{
    [Test]
    public void Controller_is_authenticated_and_exposes_long_id_master_routes()
    {
        var controller = typeof(StudentEducationIdentifierMasterController);
        var routes = controller.GetMethods().SelectMany(method => method
            .GetCustomAttributes(typeof(HttpMethodAttribute), true)
            .Cast<HttpMethodAttribute>()
            .Select(attribute => attribute.Template)).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(controller.GetCustomAttributes(typeof(AuthorizeAttribute), true), Is.Not.Empty);
            Assert.That(routes, Does.Contain("{id:long}"));
            Assert.That(routes.Count(route => route is null), Is.EqualTo(2));
        });
    }

    [Test]
    public void Filter_uses_shared_paging_defaults_and_country_filter()
    {
        var filter = new StudentEducationIdentifierMasterFilterDTO();

        Assert.Multiple(() =>
        {
            Assert.That(filter.PageNumber, Is.EqualTo(1));
            Assert.That(filter.PageSize, Is.EqualTo(10));
            Assert.That(filter.ActiveOnly, Is.True);
            Assert.That(typeof(StudentEducationIdentifierMasterFilterDTO).GetProperty(nameof(filter.CountryId)), Is.Not.Null);
        });
    }

    [Test]
    public void Persistence_model_scopes_identifier_types_to_institution_and_country()
    {
        using var context = new SkolenzaDbContext(
            new DbContextOptionsBuilder<SkolenzaDbContext>()
                .UseNpgsql("Host=localhost;Database=skolenza_model_validation;Username=postgres;Password=not-used")
                .Options);

        var entity = context.Model.FindEntityType(typeof(StudentEducationIdentifierMaster));
        var index = entity?.GetIndexes().SingleOrDefault(candidate =>
            candidate.GetDatabaseName() == "UX_StudentEducationIdentifierMaster_Institution_Country_Code");

        Assert.Multiple(() =>
        {
            Assert.That(entity?.GetTableName(), Is.EqualTo("StudentEducationIdentifierMaster"));
            Assert.That(entity?.GetSchema(), Is.EqualTo("skolenza"));
            Assert.That(index?.IsUnique, Is.True);
            Assert.That(entity?.FindProperty(nameof(StudentEducationIdentifierMaster.CountryId)), Is.Not.Null);
            Assert.That(entity?.FindProperty(nameof(StudentEducationIdentifierMaster.IssuingAuthority)), Is.Not.Null);
        });
    }

    [Test]
    public void CRUD_commands_use_admissions_permission_pipeline_and_no_numeric_permission_ids()
    {
        var resolver = typeof(StudentPermissionBehavior<object, object>)
            .GetMethod("ResolvePermission", BindingFlags.NonPublic | BindingFlags.Static);
        var commands = new object[]
        {
            new GetStudentEducationIdentifierMastersQuery(new StudentEducationIdentifierMasterFilterDTO()),
            new CreateStudentEducationIdentifierMasterCommand(new StudentEducationIdentifierMasterRequestDTO()),
            new UpdateStudentEducationIdentifierMasterCommand(1, new StudentEducationIdentifierMasterRequestDTO()),
            new DeleteStudentEducationIdentifierMasterCommand(1, new StudentEducationIdentifierMasterDeleteDTO())
        };

        foreach (var command in commands)
        {
            var permission = resolver?.Invoke(null, [command]);
            var moduleCode = permission?.GetType().GetField("Item1")?.GetValue(permission);
            var operationName = permission?.GetType().GetField("Item2")?.GetValue(permission);
            Assert.That(moduleCode, Is.EqualTo(StudentConstants.AdmissionsModuleCode), command.GetType().Name);
            Assert.That(operationName, Is.EqualTo(command switch
            {
                GetStudentEducationIdentifierMastersQuery => "View",
                CreateStudentEducationIdentifierMasterCommand => "Add",
                UpdateStudentEducationIdentifierMasterCommand => "Update",
                _ => "Delete"
            }), command.GetType().Name);
        }
    }
}
