using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Npgsql;
using NUnit.Framework;
using skolenza.application.Common.Enums;
using skolenza.application.DTOS.Student;
using skolenza.application.Exceptions;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using skolenza.persistance.Repositories;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("StudentAdmissionConcurrencyDatabase")]
public sealed class StudentAdmissionConcurrencyDatabaseTests
{
    [TestCase(0, TestName = "Competing transition rechecks admission status")]
    [TestCase(1, TestName = "Competing admission edit rechecks admission status")]
    [TestCase(2, TestName = "Competing Guardian link rechecks admission status")]
    [TestCase(3, TestName = "Competing Guardian link edit rechecks admission status")]
    [TestCase(4, TestName = "Competing Guardian unlink rechecks admission status")]
    [TestCase(5, TestName = "Competing document upload rechecks terminal admission status")]
    [TestCase(6, TestName = "Competing document review rechecks the current decision")]
    [TestCase(7, TestName = "Competing fee quote rechecks terminal admission status")]
    [TestCase(8, TestName = "Uncontended Guardian document and admission operations succeed")]
    public async Task Competing_admission_write_rechecks_status_after_row_lock(int writeKind)
    {
        var connectionString = Environment.GetEnvironmentVariable("SKOLENZA_STUDENT_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Assert.Ignore("Requires the disposable Student concurrency database.");
        }

        var settings = new NpgsqlConnectionStringBuilder(connectionString);
        Assert.That(settings.Database, Is.EqualTo("student_concurrency_codex_20261029"),
            "This test may only run against the explicitly created disposable database.");
        Assert.That(settings.Host, Is.AnyOf("localhost", "127.0.0.1", "::1"));

        var options = new DbContextOptionsBuilder<SkolenzaDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        await using var setup = new SkolenzaDbContext(options);
        var scope = await (from academicGroup in setup.AcademicGroups.AsNoTracking()
                           join campus in setup.Campuss.AsNoTracking()
                               on academicGroup.InstitutionId equals campus.InstitutionId
                           join session in setup.AcademicSessions.AsNoTracking()
                               on academicGroup.AcademicSessionId equals session.Id
                           where academicGroup.IsActive && !academicGroup.IsSoftDeleted &&
                               campus.IsActive && !campus.IsSoftDeleted &&
                               session.IsActive && !session.IsSoftDeleted &&
                               session.InstitutionId == academicGroup.InstitutionId
                           select new
                           {
                               academicGroup.InstitutionId,
                               GroupId = academicGroup.Id,
                               SessionId = academicGroup.AcademicSessionId,
                               CampusId = campus.Id,
                               campus.CountryId
                           }).FirstAsync();
        var genderId = await setup.Genders.AsNoTracking().Select(gender => gender.Id).FirstAsync();
        var admission = new StudentAdmission
        {
            InstitutionId = scope.InstitutionId,
            ApplicationNumber = $"CONCURRENCY-{Guid.NewGuid():N}",
            AcademicSessionId = scope.SessionId,
            CampusId = scope.CampusId,
            AcademicGroupId = scope.GroupId,
            FirstName = "Test",
            LastName = "Admission",
            DateOfBirth = new DateOnly(2015, 1, 1),
            GenderId = genderId,
            CountryId = scope.CountryId,
            Status = (short)StudentAdmissionStatus.Draft,
            AddedById = 1,
            AddedDateTime = DateTime.UtcNow
        };
        setup.StudentAdmissions.Add(admission);
        var guardian = new Guardian
        {
            InstitutionId = scope.InstitutionId,
            FirstName = "Test",
            LastName = "Guardian",
            IsActive = true,
            AddedById = 1,
            AddedDateTime = DateTime.UtcNow
        };
        setup.Guardians.Add(guardian);
        await setup.SaveChangesAsync();
        StudentAdmissionGuardian? existingLink = null;
        if (writeKind is 3 or 4)
        {
            existingLink = new StudentAdmissionGuardian
            {
                InstitutionId = scope.InstitutionId,
                StudentAdmissionId = admission.Id,
                GuardianId = guardian.Id,
                RelationshipType = (short)StudentGuardianRelationshipType.Mother,
                IsPrimary = true,
                HasPortalAccess = true,
                EffectiveFrom = new DateOnly(2026, 1, 1),
                IsActive = true,
                AddedById = 1,
                AddedDateTime = DateTime.UtcNow
            };
            setup.StudentAdmissionGuardians.Add(existingLink);
            await setup.SaveChangesAsync();
        }
        StudentDocumentRequirementMaster? requirement = null;
        StudentAdmissionDocument? document = null;
        if (writeKind is 5 or 6 or 8)
        {
            requirement = new StudentDocumentRequirementMaster
            {
                InstitutionId = scope.InstitutionId,
                Code = $"CONCURRENCY-{Guid.NewGuid():N}",
                Name = "Test document",
                AppliesTo = (short)StudentDocumentPersonScope.Student,
                IsRequired = true,
                AllowedMimeTypesJson = "[\"application/pdf\"]",
                MaxFileSizeBytes = 1024,
                IsActive = true,
                AddedById = 1,
                AddedDateTime = DateTime.UtcNow
            };
            setup.StudentDocumentRequirementMasters.Add(requirement);
            await setup.SaveChangesAsync();
            if (writeKind == 6)
            {
                document = new StudentAdmissionDocument
                {
                    InstitutionId = scope.InstitutionId,
                    StudentAdmissionId = admission.Id,
                    RequirementId = requirement.Id,
                    PersonScope = (short)StudentDocumentPersonScope.Student,
                    VersionNumber = 1,
                    OriginalFileName = "evidence.pdf",
                    ObjectKey = $"test/{Guid.NewGuid():N}",
                    ContentType = "application/pdf",
                    FileSizeBytes = 1,
                    ChecksumSha256 = new string('a', 64),
                    Status = (short)StudentDocumentReviewStatus.AwaitingReview,
                    SubmittedById = 1,
                    SubmittedDateTime = DateTime.UtcNow
                };
                setup.StudentAdmissionDocuments.Add(document);
                await setup.SaveChangesAsync();
            }
        }

        if (writeKind == 8)
        {
            await using (var linkContext = new SkolenzaDbContext(options))
            {
                var registry = new StudentRegistryRepository(linkContext, null!);
                await registry.AddAdmissionGuardianAsync(
                    scope.InstitutionId, 1, admission.Id,
                    new StudentAdmissionGuardianRequestDTO
                    {
                        GuardianId = guardian.Id,
                        RelationshipType = StudentGuardianRelationshipType.Mother,
                        IsPrimary = true,
                        EffectiveFrom = new DateOnly(2026, 1, 1)
                    }, CancellationToken.None);
            }

            long documentId;
            using (var stream = new MemoryStream([1]))
            await using (var uploadContext = new SkolenzaDbContext(options))
            {
                var documents = new StudentAdmissionDocumentRepository(uploadContext);
                var saved = await documents.CreateVersionAsync(
                    scope.InstitutionId, 1, admission.Id,
                    new StudentAdmissionDocumentUploadDTO
                    {
                        RequirementId = requirement!.Id,
                        PersonScope = StudentDocumentPersonScope.Student,
                        File = new FormFile(stream, 0, 1, "file", "evidence.pdf")
                    }, $"test/{Guid.NewGuid():N}", "application/pdf", new string('a', 64), CancellationToken.None);
                documentId = saved.Id;
            }

            await using (var reviewContext = new SkolenzaDbContext(options))
            {
                var documents = new StudentAdmissionDocumentRepository(reviewContext);
                await documents.ReviewAsync(scope.InstitutionId, 1, admission.Id, documentId,
                    new StudentAdmissionDocumentReviewDTO { Status = StudentDocumentReviewStatus.Approved }, CancellationToken.None);
            }

            await using (var submitContext = new SkolenzaDbContext(options))
            {
                var registry = new StudentRegistryRepository(submitContext, null!);
                await registry.ReviewAdmissionAsync(scope.InstitutionId, 1, admission.Id,
                    new StudentAdmissionReviewDTO { Status = StudentAdmissionStatus.Submitted }, CancellationToken.None);
            }

            await using var finalContext = new SkolenzaDbContext(options);
            Assert.Multiple(() =>
            {
                Assert.That(finalContext.StudentAdmissionGuardians.Count(item => item.StudentAdmissionId == admission.Id), Is.EqualTo(1));
                Assert.That(finalContext.StudentAdmissionDocuments.Single(item => item.Id == documentId).Status,
                    Is.EqualTo((short)StudentDocumentReviewStatus.Approved));
                Assert.That(finalContext.StudentAdmissions.Single(item => item.Id == admission.Id).Status,
                    Is.EqualTo((short)StudentAdmissionStatus.Submitted));
            });
            return;
        }

        await using var blocker = new SkolenzaDbContext(options);
        await using var transaction = await blocker.Database.BeginTransactionAsync();
        var firstTransition = await blocker.StudentAdmissions
            .FromSqlInterpolated($"SELECT * FROM skolenza.\"StudentAdmission\" WHERE \"Id\" = {admission.Id} FOR UPDATE")
            .SingleAsync();
        var firstStatus = writeKind is 5 or 7 ? StudentAdmissionStatus.Approved : StudentAdmissionStatus.Submitted;
        firstTransition.Status = (short)firstStatus;
        blocker.StudentAdmissionStatusHistories.Add(new StudentAdmissionStatusHistory
        {
            InstitutionId = scope.InstitutionId,
            StudentAdmissionId = admission.Id,
            FromStatus = (short)StudentAdmissionStatus.Draft,
            ToStatus = (short)firstStatus,
            ChangedById = 1,
            ChangedDateTime = DateTime.UtcNow
        });
        if (document is not null)
        {
            var firstDocumentDecision = await blocker.StudentAdmissionDocuments
                .SingleAsync(item => item.Id == document.Id);
            firstDocumentDecision.Status = (short)StudentDocumentReviewStatus.Approved;
            blocker.StudentAdmissionDocumentReviewHistories.Add(new StudentAdmissionDocumentReviewHistory
            {
                InstitutionId = scope.InstitutionId,
                StudentAdmissionDocumentId = document.Id,
                FromStatus = (short)StudentDocumentReviewStatus.AwaitingReview,
                ToStatus = (short)StudentDocumentReviewStatus.Approved,
                ChangedById = 1,
                ChangedDateTime = DateTime.UtcNow
            });
        }
        await blocker.SaveChangesAsync();

        var contenderSettings = new NpgsqlConnectionStringBuilder(connectionString)
        {
            ApplicationName = $"student-concurrency-{Guid.NewGuid():N}"
        };
        var contenderOptions = new DbContextOptionsBuilder<SkolenzaDbContext>()
            .UseNpgsql(contenderSettings.ConnectionString)
            .Options;
        await using var contender = new SkolenzaDbContext(contenderOptions);
        var repository = new StudentRegistryRepository(contender, null!);
        var documentRepository = new StudentAdmissionDocumentRepository(contender);
        var feeRepository = new StudentFeeLedgerRepository(contender);
        using var fileContents = new MemoryStream([1]);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        Task competingWrite = writeKind switch
        {
            1 => repository.UpdateAdmissionAsync(
                scope.InstitutionId, 1, admission.Id,
                new StudentAdmissionRequestDTO
                {
                    AcademicSessionId = scope.SessionId,
                    CampusId = scope.CampusId,
                    AcademicGroupId = scope.GroupId,
                    FirstName = "Changed",
                    LastName = "Admission",
                    DateOfBirth = admission.DateOfBirth,
                    GenderId = genderId,
                    CountryId = scope.CountryId
                }, timeout.Token),
            2 => repository.AddAdmissionGuardianAsync(
                scope.InstitutionId, 1, admission.Id,
                new StudentAdmissionGuardianRequestDTO
                {
                    GuardianId = guardian.Id,
                    RelationshipType = StudentGuardianRelationshipType.Mother,
                    IsPrimary = true,
                    EffectiveFrom = new DateOnly(2026, 1, 1)
                }, timeout.Token),
            3 => repository.UpdateAdmissionGuardianAsync(
                scope.InstitutionId, 1, admission.Id, existingLink!.Id,
                new StudentAdmissionGuardianRequestDTO
                {
                    GuardianId = guardian.Id,
                    RelationshipType = StudentGuardianRelationshipType.Father,
                    IsPrimary = false,
                    HasPortalAccess = false,
                    EffectiveFrom = new DateOnly(2026, 1, 1)
                }, timeout.Token),
            4 => repository.RemoveAdmissionGuardianAsync(
                scope.InstitutionId, admission.Id, existingLink!.Id,
                new DateOnly(2026, 2, 1), timeout.Token),
            5 => documentRepository.CreateVersionAsync(
                scope.InstitutionId, 1, admission.Id,
                new StudentAdmissionDocumentUploadDTO
                {
                    RequirementId = requirement!.Id,
                    PersonScope = StudentDocumentPersonScope.Student,
                    File = new FormFile(fileContents, 0, 1, "file", "evidence.pdf")
                }, $"test/{Guid.NewGuid():N}", "application/pdf", new string('a', 64), timeout.Token),
            6 => documentRepository.ReviewAsync(
                scope.InstitutionId, 1, admission.Id, document!.Id,
                new StudentAdmissionDocumentReviewDTO
                {
                    Status = StudentDocumentReviewStatus.Rejected,
                    Reason = "Test decision"
                }, timeout.Token),
            7 => feeRepository.CreateQuoteAsync(
                admission.Id, scope.InstitutionId, 1,
                new StudentAdmissionFeeQuoteRequestDTO(), timeout.Token),
            _ => repository.ReviewAdmissionAsync(
                scope.InstitutionId, 1, admission.Id,
                new StudentAdmissionReviewDTO { Status = StudentAdmissionStatus.Submitted }, timeout.Token)
        };

        await using var observer = new NpgsqlConnection(connectionString);
        await observer.OpenAsync(timeout.Token);
        var waiting = false;
        for (var attempt = 0; attempt < 100 && !waiting; attempt++)
        {
            await using var command = new NpgsqlCommand(
                "SELECT EXISTS (SELECT 1 FROM pg_stat_activity WHERE application_name = @name AND wait_event_type = 'Lock')",
                observer);
            command.Parameters.AddWithValue("name", contenderSettings.ApplicationName);
            waiting = (bool)(await command.ExecuteScalarAsync(timeout.Token))!;
            if (!waiting)
            {
                await Task.Delay(100, timeout.Token);
            }
        }

        Assert.That(waiting, Is.True, "The competing request must actually wait on the held admission row.");
        await transaction.CommitAsync(timeout.Token);
        if (writeKind == 7)
        {
            Assert.ThrowsAsync<ValidationErrorException>(async () => await competingWrite);
        }
        else
        {
            Assert.ThrowsAsync<ConflictException>(async () => await competingWrite);
        }

        await using var verification = new SkolenzaDbContext(options);
        var status = await verification.StudentAdmissions.AsNoTracking()
            .Where(item => item.Id == admission.Id)
            .Select(item => new { item.Status, item.FirstName })
            .SingleAsync();
        var historyCount = await verification.StudentAdmissionStatusHistories.AsNoTracking()
            .CountAsync(item => item.StudentAdmissionId == admission.Id);
        var guardianLinkCount = await verification.StudentAdmissionGuardians.AsNoTracking()
            .CountAsync(item => item.StudentAdmissionId == admission.Id);
        var savedLink = await verification.StudentAdmissionGuardians.AsNoTracking()
            .SingleOrDefaultAsync(item => item.StudentAdmissionId == admission.Id);
        var savedDocument = await verification.StudentAdmissionDocuments.AsNoTracking()
            .SingleOrDefaultAsync(item => item.StudentAdmissionId == admission.Id);
        var documentReviewCount = document is null
            ? 0
            : await verification.StudentAdmissionDocumentReviewHistories.AsNoTracking()
                .CountAsync(item => item.StudentAdmissionDocumentId == document.Id);
        var quoteCount = await verification.StudentAdmissionFeeQuotes.AsNoTracking()
            .CountAsync(item => item.StudentAdmissionId == admission.Id);
        Assert.Multiple(() =>
        {
            Assert.That(status.Status, Is.EqualTo((short)firstStatus));
            Assert.That(status.FirstName, Is.EqualTo("Test"), "An edit must not overwrite a submitted admission.");
            Assert.That(historyCount, Is.EqualTo(1), "The stale transition must not add a duplicate history event.");
            Assert.That(quoteCount, Is.Zero, "A quote must not be created after approval.");
            Assert.That(guardianLinkCount, Is.EqualTo(writeKind is 3 or 4 ? 1 : 0),
                "Guardian links must not be added or removed after submission.");
            if (savedLink is not null)
            {
                Assert.That(savedLink.IsActive && savedLink.IsPrimary && savedLink.HasPortalAccess, Is.True);
                Assert.That(savedLink.RelationshipType, Is.EqualTo((short)StudentGuardianRelationshipType.Mother));
            }
            if (writeKind == 5)
            {
                Assert.That(savedDocument, Is.Null, "A document must not be added after approval.");
            }
            if (writeKind == 6)
            {
                Assert.That(savedDocument?.Status, Is.EqualTo((short)StudentDocumentReviewStatus.Approved));
                Assert.That(documentReviewCount, Is.EqualTo(1), "The stale review must not add another decision.");
            }
        });
    }
}
