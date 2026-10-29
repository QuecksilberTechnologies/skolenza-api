using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NUnit.Framework;
using skolenza.application.Common.Enums;
using skolenza.application.Constants;
using skolenza.application.DTOS.Student;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using skolenza.persistance.Repositories;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("StudentAdmissionApprovalDatabase")]
public sealed class StudentAdmissionApprovalDatabaseTests
{
    [TestCase(0, TestName = "Approval commits enrollment and guardian with mocked invitation")]
    [TestCase(1, TestName = "Approval rolls back when invitation queue fails")]
    [TestCase(2, TestName = "Approval rejects a future primary guardian")]
    [TestCase(3, TestName = "Approval persists the real invitation queue and token in a disposable database")]
    public async Task Approval_is_atomic_and_requires_a_current_primary_guardian(int scenario)
    {
        var connection = Environment.GetEnvironmentVariable("SKOLENZA_STUDENT_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(connection))
        {
            Assert.Ignore("Requires the disposable Student concurrency database.");
        }

        var settings = new NpgsqlConnectionStringBuilder(connection);
        Assert.That(settings.Database, Is.EqualTo("student_concurrency_codex_20261029"));
        Assert.That(settings.Host, Is.AnyOf("localhost", "127.0.0.1", "::1"));
        var options = new DbContextOptionsBuilder<SkolenzaDbContext>().UseNpgsql(connection).Options;
        await using var setup = new SkolenzaDbContext(options);
        var scope = await (from academicGroup in setup.AcademicGroups.AsNoTracking()
                           join campus in setup.Campuss.AsNoTracking() on academicGroup.InstitutionId equals campus.InstitutionId
                           join session in setup.AcademicSessions.AsNoTracking() on academicGroup.AcademicSessionId equals session.Id
                           where academicGroup.IsActive && !academicGroup.IsSoftDeleted && campus.IsActive && !campus.IsSoftDeleted &&
                                 session.IsActive && !session.IsSoftDeleted && session.InstitutionId == academicGroup.InstitutionId
                           select new { academicGroup.InstitutionId, GroupId = academicGroup.Id, SessionId = session.Id, CampusId = campus.Id, campus.CountryId })
            .FirstAsync();
        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);
        var gender = await setup.Genders.AsNoTracking().Select(item => item.Id).FirstAsync();
        var admission = new StudentAdmission
        {
            InstitutionId = scope.InstitutionId,
            ApplicationNumber = $"APPROVAL-{Guid.NewGuid():N}",
            AcademicSessionId = scope.SessionId,
            CampusId = scope.CampusId,
            AcademicGroupId = scope.GroupId,
            FirstName = "Test",
            LastName = "Student",
            DateOfBirth = new DateOnly(2015, 1, 1),
            GenderId = gender,
            CountryId = scope.CountryId,
            Status = (short)StudentAdmissionStatus.UnderReview,
            AddedById = 1,
            AddedDateTime = now
        };
        var guardian = new Guardian
        {
            InstitutionId = scope.InstitutionId,
            FirstName = "Test",
            LastName = "Guardian",
            Email = $"guardian-{Guid.NewGuid():N}@example.test",
            IsActive = true,
            AddedById = 1,
            AddedDateTime = now
        };
        setup.StudentAdmissions.Add(admission);
        setup.Guardians.Add(guardian);
        await setup.SaveChangesAsync();
        setup.StudentAdmissionGuardians.Add(new StudentAdmissionGuardian
        {
            InstitutionId = scope.InstitutionId,
            StudentAdmissionId = admission.Id,
            GuardianId = guardian.Id,
            RelationshipType = (short)StudentGuardianRelationshipType.Mother,
            IsPrimary = true,
            HasPortalAccess = true,
            EffectiveFrom = scenario == 2 ? today.AddDays(1) : today,
            IsActive = true,
            AddedById = 1,
            AddedDateTime = now
        });

        // The restored scratch database alone is changed to isolate this fixture's required document.
        await setup.StudentDocumentRequirementMasters.Where(item => item.InstitutionId == scope.InstitutionId && item.IsRequired)
            .ExecuteUpdateAsync(update => update.SetProperty(item => item.IsRequired, false));
        var requirement = new StudentDocumentRequirementMaster
        {
            InstitutionId = scope.InstitutionId,
            Code = $"APPROVAL-{Guid.NewGuid():N}",
            Name = "Test evidence",
            AppliesTo = (short)StudentDocumentPersonScope.Student,
            IsRequired = true,
            AllowedMimeTypesJson = "[\"application/pdf\"]",
            MaxFileSizeBytes = 1024,
            IsActive = true,
            AddedById = 1,
            AddedDateTime = now
        };
        setup.StudentDocumentRequirementMasters.Add(requirement);
        await setup.SaveChangesAsync();
        setup.ChangeTracker.Clear();
        var currency = await setup.CountryCurrencyMasters.FirstOrDefaultAsync(item => item.CountryId == scope.CountryId && item.CurrencyCode == "TST");
        if (currency is null)
        {
            currency = new CountryCurrencyMaster
            {
                CountryId = scope.CountryId,
                CurrencyCode = "TST",
                CurrencyName = "Test",
                CurrencySymbol = "T",
                EffectiveFrom = new DateOnly(2000, 1, 1),
                AddedByHostUserId = 1,
                AddedDateTime = now
            };
            setup.CountryCurrencyMasters.Add(currency);
        }
        var frequency = new StudentFeeFrequencyMaster
        {
            InstitutionId = scope.InstitutionId,
            Code = $"APPROVAL-{Guid.NewGuid():N}",
            Name = "Annual test",
            InstallmentsPerYear = 1,
            IsActive = true,
            AddedById = 1,
            AddedDateTime = now
        };
        setup.StudentFeeFrequencyMasters.Add(frequency);
        await setup.SaveChangesAsync();
        setup.StudentAdmissionDocuments.Add(new StudentAdmissionDocument
        {
            InstitutionId = scope.InstitutionId,
            StudentAdmissionId = admission.Id,
            RequirementId = requirement.Id,
            PersonScope = (short)StudentDocumentPersonScope.Student,
            VersionNumber = 1,
            OriginalFileName = "test.pdf",
            ObjectKey = $"test/{Guid.NewGuid():N}",
            ContentType = "application/pdf",
            FileSizeBytes = 1,
            ChecksumSha256 = new string('a', 64),
            Status = (short)StudentDocumentReviewStatus.Approved,
            SubmittedById = 1,
            SubmittedDateTime = now
        });
        var structure = await setup.StudentFeeStructures.FirstOrDefaultAsync(item => item.InstitutionId == scope.InstitutionId && item.CampusId == scope.CampusId && item.AcademicSessionId == scope.SessionId && item.AcademicGroupId == scope.GroupId && !item.IsSoftDeleted);
        if (structure is null)
        {
            structure = new StudentFeeStructure
            {
                InstitutionId = scope.InstitutionId,
                CampusId = scope.CampusId,
                AcademicSessionId = scope.SessionId,
                AcademicGroupId = scope.GroupId,
                CountryCurrencyMasterId = currency.Id,
                CurrencyCode = "TST",
                AddedById = 1,
                AddedDateTime = now
            };
            setup.StudentFeeStructures.Add(structure);
            await setup.SaveChangesAsync();
        }
        var allowedFrequency = new StudentFeeStructureFrequency
        {
            InstitutionId = scope.InstitutionId,
            StudentFeeStructureId = structure.Id,
            FeeFrequencyMasterId = frequency.Id
        };
        setup.StudentFeeStructureFrequencies.Add(allowedFrequency);
        await setup.SaveChangesAsync();
        var quote = new StudentAdmissionFeeQuote
        {
            InstitutionId = scope.InstitutionId,
            StudentAdmissionId = admission.Id,
            StudentFeeStructureId = structure.Id,
            StudentFeeStructureFrequencyId = allowedFrequency.Id,
            CurrencyCode = "TST",
            GrossRecurringAmount = 100,
            NetAnnualAmount = 100,
            InstallmentsPerYear = 1,
            CreatedById = 1,
            CreatedDateTime = now
        };
        setup.StudentAdmissionFeeQuotes.Add(quote);
        await setup.SaveChangesAsync();
        setup.StudentFeeInstallments.Add(new StudentFeeInstallment
        {
            InstitutionId = scope.InstitutionId,
            StudentAdmissionFeeQuoteId = quote.Id,
            InstallmentNumber = 1,
            DueDate = today,
            RecurringAmount = 100,
            Status = (short)StudentFeeInstallmentStatus.Due
        });
        await setup.SaveChangesAsync();

        if (scenario == 3)
        {
            // A synthetic verified domain is valid only inside the guarded disposable database.
            setup.InstitutionPortalDomainMasters.Add(new InstitutionPortalDomainMaster
            {
                InstitutionId = scope.InstitutionId,
                DomainName = $"test-{Guid.NewGuid():N}.example.test",
                ChallengeValueHash = new string('a', 64),
                ChallengeExpiresAt = now.AddHours(1),
                VerificationStatus = (short)StudentPortalDomainVerificationStatus.Verified,
                VerifiedAt = now,
                IsActive = true,
                AddedById = 1,
                AddedDateTime = now
            });
            await setup.SaveChangesAsync();
        }

        var calls = 0;
        var portal = DispatchProxy.Create<IStudentPortalAuthRepository, BulkImportPermissionTests.TestProxy>();
        ((BulkImportPermissionTests.TestProxy)(object)portal).InvokeMethod = (_, _) =>
        {
            calls++;
            return scenario == 1 ? Task.FromException(new InvalidOperationException("Synthetic invitation failure")) : Task.CompletedTask;
        };
        await using (var context = new SkolenzaDbContext(options))
        {
            IStudentPortalAuthRepository selectedPortal = scenario == 3
                ? new StudentPortalAuthRepository(context, new EmailQueueRepository(context))
                : portal;
            var repository = new StudentRegistryRepository(context, selectedPortal);
            var request = new StudentAdmissionReviewDTO { Status = StudentAdmissionStatus.Approved };
            if (scenario is 0 or 3)
            {
                await repository.ReviewAdmissionAsync(scope.InstitutionId, 1, admission.Id, request, CancellationToken.None);
            }
            else if (scenario == 1)
            {
                Assert.ThrowsAsync<InvalidOperationException>(async () => await repository.ReviewAdmissionAsync(scope.InstitutionId, 1, admission.Id, request, CancellationToken.None));
            }
            else
            {
                var error = Assert.ThrowsAsync<ConflictException>(async () => await repository.ReviewAdmissionAsync(scope.InstitutionId, 1, admission.Id, request, CancellationToken.None));
                Assert.That(error!.Message, Does.Contain("active primary Guardian"));
            }
        }

        await using var verification = new SkolenzaDbContext(options);
        var stored = await verification.StudentAdmissions.AsNoTracking().SingleAsync(item => item.Id == admission.Id);
        var expectedStudentCode = $"{StudentConstants.StudentCodePrefix}-{admission.ApplicationNumber}";
        var studentCount = await verification.Students.CountAsync(item => item.StudentCode == expectedStudentCode);
        var history = await verification.StudentAdmissionStatusHistories.CountAsync(item => item.StudentAdmissionId == admission.Id);
        var approved = scenario is 0 or 3;
        Assert.That(calls, Is.EqualTo(scenario is 0 or 1 ? 1 : 0));
        Assert.That(stored.Status, Is.EqualTo((short)(approved ? StudentAdmissionStatus.Approved : StudentAdmissionStatus.UnderReview)));
        Assert.That(stored.StudentId.HasValue, Is.EqualTo(approved));
        Assert.That(studentCount, Is.EqualTo(approved ? 1 : 0));
        Assert.That(history, Is.EqualTo(approved ? 1 : 0));
        if (stored.StudentId.HasValue)
        {
            Assert.That(await verification.StudentEnrollments.CountAsync(item => item.StudentId == stored.StudentId), Is.EqualTo(1));
            Assert.That(await verification.StudentGuardians.CountAsync(item => item.StudentId == stored.StudentId && item.GuardianId == guardian.Id), Is.EqualTo(1));
        }
        if (scenario == 3)
        {
            Assert.That(await verification.StudentPortalOneTimeTokens.CountAsync(item => item.GuardianId == guardian.Id && item.InstitutionId == scope.InstitutionId), Is.EqualTo(1));
            Assert.That(await verification.EmailQueues.CountAsync(item => item.InstitutionId == scope.InstitutionId && item.ToEmail == guardian.Email && item.TemplateCode == StudentConstants.PortalInvitationTemplateCode && item.IsSent == false), Is.EqualTo(1));
        }
    }
}
