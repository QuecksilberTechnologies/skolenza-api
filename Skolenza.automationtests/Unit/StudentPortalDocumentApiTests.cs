using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using NUnit.Framework;
using skolenza.api.Controllers.Student;
using skolenza.application.Common.Enums;
using skolenza.application.Features.StudentCmd;
using skolenza.application.Features.StudentPortalAuthCmd;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("StudentPortalDocuments")]
public sealed class StudentPortalDocumentApiTests
{
    [Test]
    public void Portal_document_routes_require_the_separate_portal_policy_and_use_long_ids()
    {
        var controller = typeof(StudentPortalDocumentsController);
        var routes = controller.GetMethods()
            .SelectMany(method => method.GetCustomAttributes(typeof(HttpMethodAttribute), true)
                .Cast<HttpMethodAttribute>().Select(attribute => attribute.Template))
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(controller.GetCustomAttributes(typeof(AuthorizeAttribute), true)
                .Cast<AuthorizeAttribute>().Single().Policy,
                Is.EqualTo(skolenza.application.Constants.StudentConstants.PortalAuthenticationPolicy));
            Assert.That(controller.GetCustomAttribute<Microsoft.AspNetCore.Mvc.RouteAttribute>()?.Template,
                Is.EqualTo("api/Student/portal/students/{studentId:long}/documents"));
            Assert.That(routes, Does.Contain("requirements"));
            Assert.That(typeof(StudentPortalDocumentsController).GetMethod(nameof(StudentPortalDocumentsController.GetPage))
                ?.GetCustomAttribute<HttpGetAttribute>()?.Template, Is.Null);
            Assert.That(routes, Does.Contain("{documentId:long}/download"));
            Assert.That(typeof(StudentAdmissionsController).GetMethods()
                .SelectMany(method => method.GetCustomAttributes(typeof(HttpMethodAttribute), true)
                    .Cast<HttpMethodAttribute>().Select(attribute => attribute.Template)),
                Does.Contain("records/{studentId:long}/documents/{documentId:long}/review"));
            Assert.That(typeof(ReviewStudentRecordDocumentCommand).Namespace,
                Is.EqualTo("skolenza.application.Features.StudentCmd"));
        });
    }

    [Test]
    public void Portal_document_entities_have_tenant_scope_versioning_and_auditable_actor_constraints()
    {
        using var context = new SkolenzaDbContext(
            new DbContextOptionsBuilder<SkolenzaDbContext>()
                .UseNpgsql("Host=localhost;Database=skolenza_portal_document_model_validation;Username=postgres;Password=not-used")
                .Options);

        var model = context.GetService<IDesignTimeModel>().Model;
        var document = model.FindEntityType(typeof(StudentRecordDocument));
        var history = model.FindEntityType(typeof(StudentRecordDocumentReviewHistory));
        var documentConstraints = document?.GetCheckConstraints().Select(item => item.Name).ToArray();
        var historyConstraints = history?.GetCheckConstraints().Select(item => item.Name).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(document?.GetSchema(), Is.EqualTo("skolenza"));
            Assert.That(document?.GetTableName(), Is.EqualTo("StudentRecordDocument"));
            Assert.That(document?.FindProperty(nameof(StudentRecordDocument.Id))?.ClrType, Is.EqualTo(typeof(long)));
            Assert.That(document?.GetIndexes().Any(index => index.IsUnique &&
                index.GetDatabaseName() == "UX_StudentRecordDocument_Student_Version"), Is.True);
            Assert.That(documentConstraints, Does.Contain("CK_StudentRecordDocument_PersonScope"));
            Assert.That(documentConstraints, Does.Contain("CK_StudentRecordDocument_GuardianScope"));
            Assert.That(history?.GetTableName(), Is.EqualTo("StudentRecordDocumentReviewHistory"));
            Assert.That(history?.FindProperty(nameof(StudentRecordDocumentReviewHistory.ChangedByStaffMemberId))?.IsNullable, Is.True);
            Assert.That(history?.FindProperty(nameof(StudentRecordDocumentReviewHistory.ChangedByPortalAccountId))?.IsNullable, Is.True);
            Assert.That(historyConstraints, Does.Contain("CK_StudentRecordDocumentReviewHistory_Actor"));
        });
    }

    [Test]
    public void Portal_document_registry_script_is_additive_and_has_storage_and_review_history_contract()
    {
        var root = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", ".."));
        var script = File.ReadAllText(Path.Combine(root, "database-scripts", "CreateStudentPortalDocuments.sql"));

        Assert.Multiple(() =>
        {
            Assert.That(script, Does.Contain("StudentRecordDocument"));
            Assert.That(script, Does.Contain("StudentRecordDocumentReviewHistory"));
            Assert.That(script, Does.Contain("ObjectKey"));
            Assert.That(script, Does.Contain("ChecksumSha256"));
            Assert.That(script, Does.Contain("CK_StudentRecordDocumentReviewHistory_Actor"));
            Assert.That(script, Does.Contain("CreateStudentAdmissionsAndRecords.sql"));
            Assert.That(script, Does.Contain("CreateStudentPortalAuthentication.sql"));
            Assert.That(script, Does.Not.Contain("DROP TABLE"));
            Assert.That(script, Does.Not.Contain("TRUNCATE"));
        });
    }

    [Test]
    public void Portal_document_person_and_review_values_come_from_central_enums()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Enum.GetValues<StudentDocumentPersonScope>(), Has.Length.EqualTo(3));
            Assert.That((short)StudentDocumentPersonScope.Student, Is.EqualTo(1));
            Assert.That((short)StudentDocumentPersonScope.Guardian, Is.EqualTo(2));
            Assert.That(Enum.GetValues<StudentDocumentReviewStatus>(), Has.Length.EqualTo(4));
        });
    }
}
