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
[Category("StudentEducationIdentifier")]
public sealed class StudentEducationIdentifierApiTests
{
    [Test]
    public void Controller_is_authenticated_and_exposes_long_id_routes()
    {
        var controller = typeof(StudentEducationIdentifierController);
        var routes = controller.GetMethods().SelectMany(method => method
            .GetCustomAttributes(typeof(HttpMethodAttribute), true)
            .Cast<HttpMethodAttribute>()
            .Select(attribute => attribute.Template)).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(controller.GetCustomAttributes(typeof(AuthorizeAttribute), true), Is.Not.Empty);
            Assert.That(routes, Does.Contain("{studentId:long}/education-identifiers"));
            Assert.That(routes, Does.Contain("education-identifiers/{id:long}"));
            Assert.That(routes, Does.Contain("education-identifiers/{id:long}/verification"));
        });
    }

    [Test]
    public void Identifier_list_defaults_to_central_page_size_and_response_never_has_plaintext_value()
    {
        var filter = new StudentEducationIdentifierFilterDTO();

        Assert.Multiple(() =>
        {
            Assert.That(filter.PageNumber, Is.EqualTo(1));
            Assert.That(filter.PageSize, Is.EqualTo(10));
            Assert.That(typeof(StudentEducationIdentifierResponseDTO).GetProperty("Value"), Is.Null);
            Assert.That(typeof(StudentEducationIdentifierResponseDTO).GetProperty("EncryptedValue"), Is.Null);
            Assert.That(typeof(StudentEducationIdentifierResponseDTO).GetProperty("MaskedValue"), Is.Not.Null);
        });
    }

    [Test]
    public void Persistence_model_has_tenant_and_student_scope_and_unique_identifier_type()
    {
        using var context = new SkolenzaDbContext(
            new DbContextOptionsBuilder<SkolenzaDbContext>()
                .UseNpgsql("Host=localhost;Database=skolenza_model_validation;Username=postgres;Password=not-used")
                .Options);

        var entity = context.Model.FindEntityType(typeof(StudentEducationIdentifier));
        var index = entity?.GetIndexes().SingleOrDefault(candidate =>
            candidate.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(StudentEducationIdentifier.InstitutionId),
                    nameof(StudentEducationIdentifier.StudentId),
                    nameof(StudentEducationIdentifier.IdentifierMasterId)]));

        Assert.Multiple(() =>
        {
            Assert.That(entity?.GetTableName(), Is.EqualTo("StudentEducationIdentifier"));
            Assert.That(entity?.GetSchema(), Is.EqualTo("skolenza"));
            Assert.That(index?.IsUnique, Is.True);
            Assert.That(entity?.FindProperty(nameof(StudentEducationIdentifier.EncryptedValue)), Is.Not.Null);
            Assert.That(entity?.FindProperty(nameof(StudentEducationIdentifier.VerifiedById)), Is.Not.Null);
            Assert.That(entity?.FindProperty(nameof(StudentEducationIdentifier.UpdatedById)), Is.Not.Null);
            Assert.That(context.Model.FindEntityType(typeof(StudentEducationIdentifierVerificationHistory))?.GetTableName(),
                Is.EqualTo("StudentEducationIdentifierVerificationHistory"));
        });
    }

    [Test]
    public void Short_official_identifiers_are_fully_masked()
    {
        var mask = typeof(skolenza.persistance.Repositories.StudentEducationIdentifierRepository)
            .GetMethod("Mask", BindingFlags.NonPublic | BindingFlags.Static);
        var result = mask?.Invoke(null, ["AB12"]);

        Assert.That(result, Is.EqualTo("****"));
    }

    [Test]
    public void Identifier_commands_use_existing_admissions_permission_pipeline()
    {
        var resolver = typeof(StudentPermissionBehavior<object, object>)
            .GetMethod("ResolvePermission", BindingFlags.NonPublic | BindingFlags.Static);
        var commands = new object[]
        {
            new GetStudentEducationIdentifiersQuery(new StudentEducationIdentifierFilterDTO()),
            new CreateStudentEducationIdentifierCommand(new StudentEducationIdentifierRequestDTO()),
            new UpdateStudentEducationIdentifierCommand(1, new StudentEducationIdentifierUpdateDTO()),
            new SetStudentEducationIdentifierVerificationCommand(1, new StudentEducationIdentifierVerificationDTO())
        };

        foreach (var command in commands)
        {
            var permission = resolver?.Invoke(null, [command]);
            Assert.That(permission?.GetType().GetField("Item1")?.GetValue(permission),
                Is.EqualTo(StudentConstants.AdmissionsModuleCode), command.GetType().Name);
        }
    }
}
