using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using skolenza.api.Controllers.Student;
using skolenza.application.Common.Enums;
using skolenza.application.DTOS.Student;
using skolenza.application.Features.StudentCmd;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("StudentRegistry")]
public sealed class StudentAdmissionDocumentApiTests
{
    [Test]
    public void Document_routes_are_authenticated_and_use_long_identifiers()
    {
        var controller = typeof(StudentAdmissionsController);
        var routes = controller.GetMethods()
            .SelectMany(method => method.GetCustomAttributes(typeof(HttpMethodAttribute), true)
                .Cast<HttpMethodAttribute>().Select(attribute => attribute.Template))
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(controller.GetCustomAttributes(typeof(AuthorizeAttribute), true), Is.Not.Empty);
            Assert.That(routes, Does.Contain("admissions/{id:long}/documents"));
            Assert.That(routes, Does.Contain("admissions/{id:long}/documents/{documentId:long}/download"));
            Assert.That(routes, Does.Contain("admissions/{id:long}/documents/{documentId:long}/review"));
            Assert.That(routes, Does.Contain("admissions/{id:long}"));
            Assert.That(routes, Does.Contain("admissions/{id:long}/status-history"));
        });
    }

    [Test]
    public void Admission_status_history_uses_central_paging_defaults_and_keeps_initial_status_nullable()
    {
        var filter = new StudentAdmissionStatusHistoryFilterDTO();
        var history = new StudentAdmissionStatusHistoryResponseDTO();

        Assert.Multiple(() =>
        {
            Assert.That(filter.PageNumber, Is.EqualTo(1));
            Assert.That(filter.PageSize, Is.EqualTo(10));
            Assert.That(history.FromStatus, Is.Null);
        });
    }

    [Test]
    public async Task Document_review_statuses_are_central_and_returned_by_options()
    {
        Assert.That(Enum.GetValues<StudentDocumentReviewStatus>(), Has.Length.EqualTo(4));
        Assert.That((short)StudentDocumentReviewStatus.AwaitingReview, Is.EqualTo(1));

        var result = await new GetStudentOptionsQueryHandler().Handle(
            new GetStudentOptionsQuery(new StudentOptionsRequestDTO()), CancellationToken.None);

        Assert.That(result.Data.DocumentReviewStatuses.Select(item => item.Name),
            Is.EquivalentTo(Enum.GetNames<StudentDocumentReviewStatus>()));
    }

    [Test]
    public void Document_entities_have_bigint_keys_and_scoped_review_history()
    {
        using var context = new SkolenzaDbContext(
            new DbContextOptionsBuilder<SkolenzaDbContext>()
                .UseNpgsql("Host=localhost;Database=skolenza_model_validation;Username=postgres;Password=not-used")
                .Options);

        var document = context.Model.FindEntityType(typeof(StudentAdmissionDocument));
        var history = context.Model.FindEntityType(typeof(StudentAdmissionDocumentReviewHistory));

        Assert.Multiple(() =>
        {
            Assert.That(document?.GetSchema(), Is.EqualTo("skolenza"));
            Assert.That(document?.GetTableName(), Is.EqualTo("StudentAdmissionDocument"));
            Assert.That(document?.FindProperty(nameof(StudentAdmissionDocument.Id))?.ClrType, Is.EqualTo(typeof(long)));
            Assert.That(document?.GetIndexes().Any(index =>
                index.IsUnique && index.GetDatabaseName() == "UX_StudentAdmissionDocument_Version"), Is.True);
            Assert.That(history?.GetSchema(), Is.EqualTo("skolenza"));
            Assert.That(history?.GetTableName(), Is.EqualTo("StudentAdmissionDocumentReviewHistory"));
            Assert.That(history?.FindProperty(nameof(StudentAdmissionDocumentReviewHistory.Id))?.ClrType, Is.EqualTo(typeof(long)));
        });
    }

    [Test]
    public void Additive_registry_sql_contains_document_version_and_review_history_without_drop()
    {
        var root = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", ".."));
        var script = File.ReadAllText(Path.Combine(root, "database-scripts", "CreateStudentAdmissionsAndRecords.sql"));

        Assert.Multiple(() =>
        {
            Assert.That(script, Does.Contain("StudentAdmissionDocumentReviewHistory"));
            Assert.That(script, Does.Contain("UX_StudentAdmissionDocument_Version"));
            Assert.That(script, Does.Contain("ChecksumSha256"));
            Assert.That(script, Does.Contain("ON DELETE RESTRICT"));
            Assert.That(script, Does.Not.Contain("DROP TABLE"));
        });
    }
}
