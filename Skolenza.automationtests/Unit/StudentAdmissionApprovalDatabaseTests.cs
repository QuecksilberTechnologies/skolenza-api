using System.Reflection;
using System.Text.Json;
using System.Text;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using NUnit.Framework;
using skolenza.application.Common.Enums;
using skolenza.application.Constants;
using skolenza.application.DTOS.Student;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Interfaces.IFileStorage;
using skolenza.application.Interfaces.IHashed;
using skolenza.application.Interfaces.ITokenService;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Common.Helpers;
using skolenza.application.Features.StudentPortalAuthCmd;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using skolenza.persistance.Repositories;
using skolenza.infrastructure.EncryptionService;
using skolenza.infrastructure.Security.HashedService;
using skolenza.infrastructure.Token;
using skolenza.api.Controllers.Student;
using skolenza.api.Middlewares;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("StudentAdmissionApprovalDatabase")]
public sealed class StudentAdmissionApprovalDatabaseTests
{
    [TestCase(0, TestName = "Approval commits enrollment and guardian with mocked invitation")]
    [TestCase(1, TestName = "Approval rolls back when invitation queue fails")]
    [TestCase(2, TestName = "Approval rejects a future primary guardian")]
    [TestCase(3, TestName = "Approval persists the real invitation queue and token in a disposable database")]
    [TestCase(4, TestName = "Quote persists a reconciled installment head snapshot")]
    [TestCase(5, TestName = "Competing head payments do not exceed the installment balance")]
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
            PhoneNumber = "9999999999",
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
        if (scenario is 4 or 5)
        {
            await setup.StudentFeeStructureItems
                .Where(item => item.InstitutionId == scope.InstitutionId && item.StudentFeeStructureId == structure.Id)
                .ExecuteUpdateAsync(update => update.SetProperty(item => item.IsActive, false));
            var recurringMaster = new StudentFeeComponentMaster
            {
                InstitutionId = scope.InstitutionId,
                Code = $"RECUR-{Guid.NewGuid():N}",
                Name = "Recurring test fee",
                ChargeNature = (short)StudentFeeComponentChargeNature.RecurringAnnual,
                DisplayOrder = 2,
                IsActive = true,
                AddedById = 1,
                AddedDateTime = now
            };
            var oneTimeMaster = new StudentFeeComponentMaster
            {
                InstitutionId = scope.InstitutionId,
                Code = $"ONETIME-{Guid.NewGuid():N}",
                Name = "One-time test fee",
                ChargeNature = (short)StudentFeeComponentChargeNature.OneTime,
                DisplayOrder = 1,
                IsActive = true,
                AddedById = 1,
                AddedDateTime = now
            };
            var excludedMaster = new StudentFeeComponentMaster
            {
                InstitutionId = scope.InstitutionId,
                Code = $"EXCLUDED-{Guid.NewGuid():N}",
                Name = "Discount-excluded recurring test fee",
                ChargeNature = (short)StudentFeeComponentChargeNature.RecurringAnnual,
                DisplayOrder = 3,
                IsActive = true,
                AddedById = 1,
                AddedDateTime = now
            };
            setup.StudentFeeComponentMasters.AddRange(recurringMaster, oneTimeMaster, excludedMaster);
            await setup.SaveChangesAsync();
            setup.StudentFeeStructureItems.AddRange(
                new StudentFeeStructureItem
                {
                    InstitutionId = scope.InstitutionId,
                    StudentFeeStructureId = structure.Id,
                    FeeComponentMasterId = recurringMaster.Id,
                    ComponentCode = recurringMaster.Code,
                    ComponentName = recurringMaster.Name,
                    ChargeNature = recurringMaster.ChargeNature,
                    Amount = 100m
                },
                new StudentFeeStructureItem
                {
                    InstitutionId = scope.InstitutionId,
                    StudentFeeStructureId = structure.Id,
                    FeeComponentMasterId = oneTimeMaster.Id,
                    ComponentCode = oneTimeMaster.Code,
                    ComponentName = oneTimeMaster.Name,
                    ChargeNature = oneTimeMaster.ChargeNature,
                    Amount = 30m
                },
                new StudentFeeStructureItem
                {
                    InstitutionId = scope.InstitutionId,
                    StudentFeeStructureId = structure.Id,
                    FeeComponentMasterId = excludedMaster.Id,
                    ComponentCode = excludedMaster.Code,
                    ComponentName = excludedMaster.Name,
                    ChargeNature = excludedMaster.ChargeNature,
                    Amount = 10m,
                    IsDiscountEligible = false
                });
            setup.StudentFeeStructureInstallmentDueDates.Add(new StudentFeeStructureInstallmentDueDate
            {
                InstitutionId = scope.InstitutionId,
                StudentFeeStructureFrequencyId = allowedFrequency.Id,
                InstallmentNumber = 1,
                DueDate = today
            });
            var benefitScheme = new StudentBenefitSchemeMaster
            {
                InstitutionId = scope.InstitutionId,
                Code = $"DISCOUNT-{Guid.NewGuid():N}",
                Name = "Synthetic discount scheme",
                MinimumDiscountPercent = 0m,
                MaximumDiscountPercent = 20m,
                IsActive = true,
                AddedById = 1,
                AddedDateTime = now
            };
            setup.StudentBenefitSchemeMasters.Add(benefitScheme);
            await setup.SaveChangesAsync();
            await using var quoteContext = new SkolenzaDbContext(options);
            var repository = new StudentFeeLedgerRepository(quoteContext);
            var saved = await repository.CreateQuoteAsync(admission.Id, scope.InstitutionId, 1,
                new StudentAdmissionFeeQuoteRequestDTO
                {
                    StudentFeeStructureId = structure.Id,
                    StudentFeeStructureFrequencyId = allowedFrequency.Id,
                    StudentBenefitSchemeMasterId = benefitScheme.Id,
                    DiscountPercent = 10m
                }, CancellationToken.None);
            await using var snapshotContext = new SkolenzaDbContext(options);
            var snapshot = await snapshotContext.StudentFeeInstallmentHeads.AsNoTracking()
                .Where(item => item.InstitutionId == scope.InstitutionId && item.StudentAdmissionFeeQuoteId == saved.Id)
                .ToListAsync();
            var lines = await snapshotContext.StudentAdmissionFeeQuoteLines.AsNoTracking()
                .Where(item => item.InstitutionId == scope.InstitutionId && item.StudentAdmissionFeeQuoteId == saved.Id)
                .ToListAsync();
            Assert.That(lines.Single(item => item.ComponentCode == recurringMaster.Code).IsDiscountEligible, Is.True);
            Assert.That(lines.Single(item => item.ComponentCode == oneTimeMaster.Code).IsDiscountEligible, Is.False);
            Assert.That(lines.Single(item => item.ComponentCode == excludedMaster.Code).IsDiscountEligible, Is.False);
            Assert.That(lines.Single(item => item.ComponentCode == recurringMaster.Code).DiscountAmount, Is.EqualTo(10m));
            Assert.That(lines.Single(item => item.ComponentCode == excludedMaster.Code).DiscountAmount, Is.Zero);
            Assert.That(lines.Single(item => item.ComponentCode == oneTimeMaster.Code).DiscountAmount, Is.Zero);
            Assert.That(snapshot.Sum(item => item.ScheduledAmount), Is.EqualTo(saved.NetAnnualAmount));
            foreach (var line in lines)
            {
                Assert.That(snapshot.Where(item => item.StudentAdmissionFeeQuoteLineId == line.Id)
                    .Sum(item => item.ScheduledAmount), Is.EqualTo(line.NetAmount));
            }
            Assert.That(snapshot.Count, Is.GreaterThanOrEqualTo(2));
            // The quote owns its historical allocation order even if a school edits the master later.
            await snapshotContext.StudentFeeComponentMasters
                .Where(item => item.Id == recurringMaster.Id)
                .ExecuteUpdateAsync(update => update.SetProperty(item => item.DisplayOrder, (short)0));
            await snapshotContext.StudentFeeComponentMasters
                .Where(item => item.Id == oneTimeMaster.Id)
                .ExecuteUpdateAsync(update => update.SetProperty(item => item.DisplayOrder, (short)20));
            var installmentId = saved.Installments.Single().Id;
            if (scenario == 5)
            {
                async Task<bool> AttemptAsync()
                {
                    await using var attemptContext = new SkolenzaDbContext(options);
                    try
                    {
                        await new StudentFeeLedgerRepository(attemptContext).RecordOfflinePaymentAsync(
                            installmentId, scope.InstitutionId, 1, new StudentOfflinePaymentRequestDTO
                            {
                                IdempotencyKey = $"head-race-{Guid.NewGuid():N}",
                                Amount = 100m,
                                PaidAt = DateTimeOffset.UtcNow
                            }, CancellationToken.None);
                        return true;
                    }
                    catch (ValidationErrorException)
                    {
                        return false;
                    }
                    catch (ConflictException)
                    {
                        return false;
                    }
                }
                var results = await Task.WhenAll(AttemptAsync(), AttemptAsync());
                Assert.That(results.Count(item => item), Is.EqualTo(1));
                var storedInstallment = await snapshotContext.StudentFeeInstallments.AsNoTracking()
                    .SingleAsync(item => item.Id == installmentId);
                var storedAllocations = await snapshotContext.StudentFeePaymentAllocations.AsNoTracking()
                    .Where(item => item.InstitutionId == scope.InstitutionId && item.StudentFeeInstallmentId == installmentId)
                    .ToListAsync();
                Assert.That(storedInstallment.PaidAmount, Is.EqualTo(100m));
                Assert.That(storedAllocations.Sum(item => item.Amount), Is.EqualTo(100m));
                Assert.That(storedInstallment.PaidAmount, Is.LessThanOrEqualTo(saved.NetAnnualAmount));
                return;
            }
            var offlineRequest = new StudentOfflinePaymentRequestDTO
            {
                IdempotencyKey = $"head-payment-{Guid.NewGuid():N}",
                Amount = 85m,
                PaidAt = DateTimeOffset.UtcNow,
                RecordedReference = "synthetic-partial"
            };
            await using (var offlineContext = new SkolenzaDbContext(options))
            {
                var offline = new StudentFeeLedgerRepository(offlineContext);
                var payment = await offline.RecordOfflinePaymentAsync(
                    installmentId, scope.InstitutionId, 1, offlineRequest, CancellationToken.None);
                var replay = await offline.RecordOfflinePaymentAsync(
                    installmentId, scope.InstitutionId, 1, offlineRequest, CancellationToken.None);
                Assert.That(replay.Id, Is.EqualTo(payment.Id));
                var allocated = await snapshotContext.StudentFeePaymentAllocations.AsNoTracking()
                    .Where(item => item.StudentFeePaymentTransactionId == payment.Id)
                    .ToListAsync();
                Assert.That(allocated.Sum(item => item.Amount), Is.EqualTo(85m));
                Assert.That(allocated.All(item => item.StudentFeeInstallmentHeadId.HasValue), Is.True);
                var firstHead = snapshot.Single(item => item.AllocationPriority == 1);
                var secondHead = snapshot.Single(item => item.AllocationPriority == 2);
                Assert.That(allocated.Single(item => item.StudentFeeInstallmentHeadId == firstHead.Id).Amount,
                    Is.EqualTo(30m));
                Assert.That(allocated.Single(item => item.StudentFeeInstallmentHeadId == secondHead.Id).Amount,
                    Is.EqualTo(55m));
                var balance = await offline.GetAdmissionBalanceAsync(admission.Id, scope.InstitutionId, CancellationToken.None);
                var installment = balance.Installments.Single();
                Assert.That(installment.HeadAllocationComplete, Is.True);
                Assert.That(installment.Heads.Sum(item => item.PaidAmount), Is.EqualTo(85m));
                Assert.That(installment.Heads.Sum(item => item.OutstandingAmount),
                    Is.EqualTo(saved.NetAnnualAmount - 85m));
                var receipts = await offline.GetReceiptsAsync(scope.InstitutionId,
                    new StudentFeeReceiptListRequestDTO { StudentAdmissionId = admission.Id }, CancellationToken.None);
                var receipt = receipts.Data.Single(item => item.PaymentTransactionId == payment.Id);
                Assert.That(receipt.HeadAllocationComplete, Is.True);
                Assert.That(receipt.Allocations.Sum(item => item.Amount), Is.EqualTo(85m));
            }

            await snapshotContext.StudentFeeInstallments.Where(item => item.Id == installmentId)
                .ExecuteUpdateAsync(update => update.SetProperty(item => item.PaidAmount, 86m));
            await using (var mismatchContext = new SkolenzaDbContext(options))
            {
                var mismatch = new StudentFeeLedgerRepository(mismatchContext);
                Assert.ThrowsAsync<ConflictException>(async () => await mismatch.RecordOfflinePaymentAsync(
                    installmentId, scope.InstitutionId, 1, new StudentOfflinePaymentRequestDTO
                    {
                        IdempotencyKey = $"head-mismatch-{Guid.NewGuid():N}",
                        Amount = 1m,
                        PaidAt = DateTimeOffset.UtcNow
                    }, CancellationToken.None));
            }
            await snapshotContext.StudentFeeInstallments.Where(item => item.Id == installmentId)
                .ExecuteUpdateAsync(update => update.SetProperty(item => item.PaidAmount, 85m));
            await using (var gatewayContext = new SkolenzaDbContext(options))
            {
                var gateway = new StudentFeeLedgerRepository(gatewayContext);
                var checkout = await gateway.PrepareCashfreeCheckoutAsync(
                    installmentId, scope.InstitutionId, $"head-checkout-{Guid.NewGuid():N}", CancellationToken.None);
                var details = new StudentCashfreePaymentDetailsDTO
                {
                    OrderId = checkout.GatewayOrderId,
                    PaymentId = $"head-gateway-{Guid.NewGuid():N}",
                    Status = "SUCCESS",
                    Amount = checkout.Amount,
                    CurrencyCode = checkout.CurrencyCode
                };
                var eventId = $"head-event-{Guid.NewGuid():N}";
                var recorded = await gateway.RecordCashfreePaymentAsync(checkout.PaymentAttemptId, details,
                    eventId, "PAYMENT_SUCCESS", new string('b', 64), CancellationToken.None);
                var replay = await gateway.RecordCashfreePaymentAsync(checkout.PaymentAttemptId, details,
                    eventId, "PAYMENT_SUCCESS", new string('b', 64), CancellationToken.None);
                Assert.That(replay.PaymentTransactionId, Is.EqualTo(recorded.PaymentTransactionId));
            }
            var finalAllocations = await snapshotContext.StudentFeePaymentAllocations.AsNoTracking()
                .Where(item => item.InstitutionId == scope.InstitutionId && item.StudentFeeInstallmentId == installmentId)
                .ToListAsync();
            Assert.That(finalAllocations.Sum(item => item.Amount), Is.EqualTo(saved.NetAnnualAmount));
            foreach (var head in snapshot)
            {
                Assert.That(finalAllocations.Where(item => item.StudentFeeInstallmentHeadId == head.Id).Sum(item => item.Amount),
                    Is.EqualTo(head.ScheduledAmount));
            }
            return;
        }
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
            var existingDomain = await setup.InstitutionPortalDomainMasters.AsNoTracking()
                .SingleOrDefaultAsync(item => item.InstitutionId == scope.InstitutionId && !item.IsSoftDeleted);
            if (existingDomain is null)
            {
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
            else
            {
                Assert.That(existingDomain.DomainName, Does.EndWith(".example.test"));
                Assert.That(existingDomain.IsActive, Is.True);
                Assert.That(existingDomain.VerificationStatus, Is.EqualTo((short)StudentPortalDomainVerificationStatus.Verified));
            }
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
            var emailProtector = (IStudentPortalEmailProtector)new StudentGatewaySecretProtector(new TestInstitutionKeyResolver());
            IStudentPortalAuthRepository selectedPortal = scenario == 3
                ? new StudentPortalAuthRepository(context, new EmailQueueRepository(context), emailProtector)
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
        if (scenario == 0)
        {
            var installment = await verification.StudentFeeInstallments.AsNoTracking()
                .SingleAsync(item => item.StudentAdmissionFeeQuoteId == quote.Id);
            var firstRequest = new StudentOfflinePaymentRequestDTO
            {
                IdempotencyKey = $"offline-{Guid.NewGuid():N}",
                Amount = 40m,
                PaidAt = DateTimeOffset.UtcNow,
                RecordedReference = "synthetic-payment"
            };
            await using var paymentContext = new SkolenzaDbContext(options);
            var ledger = new StudentFeeLedgerRepository(paymentContext);
            var guardianAdmissions = await ledger.GetPortalAdmissionsAsync(
                scope.InstitutionId, stored.StudentId!.Value, new StudentPortalFeeFilterDTO(), CancellationToken.None);
            Assert.That(guardianAdmissions.Data.Select(item => item.AdmissionId), Does.Contain(admission.Id));
            Assert.That(await ledger.AdmissionBelongsToStudentAsync(
                scope.InstitutionId, stored.StudentId.Value, admission.Id, CancellationToken.None), Is.True);
            Assert.That(await ledger.AdmissionBelongsToStudentAsync(
                scope.InstitutionId, stored.StudentId.Value + 100000, admission.Id, CancellationToken.None), Is.False);
            var guardianQuote = await ledger.GetAdmissionQuoteAsync(admission.Id, scope.InstitutionId, CancellationToken.None);
            Assert.That(guardianQuote.Id, Is.EqualTo(quote.Id));
            Assert.That(await ledger.InstallmentBelongsToAdmissionAsync(
                scope.InstitutionId, admission.Id, installment.Id, CancellationToken.None), Is.True);
            Assert.That(await ledger.InstallmentBelongsToAdmissionAsync(
                scope.InstitutionId, admission.Id + 100000, installment.Id, CancellationToken.None), Is.False);
            var checkoutKey = $"checkout-{Guid.NewGuid():N}";
            var checkout = await ledger.PrepareCashfreeCheckoutAsync(installment.Id, scope.InstitutionId, checkoutKey, CancellationToken.None);
            var checkoutReplay = await ledger.PrepareCashfreeCheckoutAsync(installment.Id, scope.InstitutionId, checkoutKey, CancellationToken.None);
            Assert.Multiple(() =>
            {
                Assert.That(checkout.Amount, Is.EqualTo(100m));
                Assert.That(checkoutReplay.PaymentAttemptId, Is.EqualTo(checkout.PaymentAttemptId));
            });
            var expiry = DateTimeOffset.UtcNow.AddHours(1);
            await ledger.SaveCashfreeOrderAsync(checkout.PaymentAttemptId, scope.InstitutionId,
                "synthetic-session", expiry, CancellationToken.None);
            var savedCheckout = await ledger.PrepareCashfreeCheckoutAsync(installment.Id, scope.InstitutionId, checkoutKey, CancellationToken.None);
            Assert.That(savedCheckout.ExpiresAt, Is.Not.Null);
            Assert.That(Math.Abs((savedCheckout.ExpiresAt!.Value - expiry).TotalMilliseconds), Is.LessThan(1));
            var failedEventId = $"failed-{Guid.NewGuid():N}";
            var failedPayment = new StudentCashfreePaymentDetailsDTO
            {
                OrderId = checkout.GatewayOrderId,
                PaymentId = $"payment-{Guid.NewGuid():N}",
                Status = "FAILED",
                Amount = checkout.Amount,
                CurrencyCode = checkout.CurrencyCode
            };
            var failedStatus = await ledger.RecordCashfreePaymentAsync(checkout.PaymentAttemptId,
                failedPayment, failedEventId, "PAYMENT_FAILED", new string('a', 64), CancellationToken.None);
            var failedReplay = await ledger.RecordCashfreePaymentAsync(checkout.PaymentAttemptId,
                failedPayment, failedEventId, "PAYMENT_FAILED", new string('a', 64), CancellationToken.None);
            Assert.That(failedReplay.AttemptStatus, Is.EqualTo(failedStatus.AttemptStatus));
            Assert.That(await paymentContext.StudentFeePaymentWebhookEvents.AsNoTracking()
                .CountAsync(item => item.EventId == failedEventId), Is.EqualTo(1));
            var paymentApplications = new[] { $"fee-test-{Guid.NewGuid():N}", $"fee-test-{Guid.NewGuid():N}" };
            await using var paymentBlocker = new NpgsqlConnection(connection);
            await paymentBlocker.OpenAsync();
            await using var paymentBlockingTransaction = await paymentBlocker.BeginTransactionAsync();
            await using (var lockCommand = new NpgsqlCommand(
                "SELECT pg_advisory_xact_lock(hashtextextended(@key, 0))", paymentBlocker, paymentBlockingTransaction))
            {
                lockCommand.Parameters.AddWithValue("key", $"{scope.InstitutionId}:{firstRequest.IdempotencyKey}");
                await lockCommand.ExecuteNonQueryAsync();
            }

            async Task<StudentFeePaymentResponseDTO> PayConcurrentlyAsync(string applicationName)
            {
                var requestConnection = new NpgsqlConnectionStringBuilder(connection) { ApplicationName = applicationName };
                var requestOptions = new DbContextOptionsBuilder<SkolenzaDbContext>().UseNpgsql(requestConnection.ConnectionString).Options;
                await using var requestContext = new SkolenzaDbContext(requestOptions);
                return await new StudentFeeLedgerRepository(requestContext).RecordOfflinePaymentAsync(
                    installment.Id, scope.InstitutionId, 1, firstRequest, CancellationToken.None);
            }

            var concurrentPayments = paymentApplications.Select(PayConcurrentlyAsync).ToArray();
            var paymentsWaiting = false;
            try
            {
                await using var monitor = new NpgsqlConnection(connection);
                await monitor.OpenAsync();
                var deadline = DateTime.UtcNow.AddSeconds(15);
                while (DateTime.UtcNow < deadline)
                {
                    await using var waiting = new NpgsqlCommand(
                        "SELECT count(*) FROM pg_stat_activity WHERE application_name = ANY(@names) AND wait_event_type = 'Lock'", monitor);
                    waiting.Parameters.AddWithValue("names", paymentApplications);
                    if (Convert.ToInt32(await waiting.ExecuteScalarAsync()) == 2)
                    {
                        paymentsWaiting = true;
                        break;
                    }
                    await Task.Delay(50);
                }
            }
            finally
            {
                await paymentBlockingTransaction.CommitAsync();
            }

            var payments = await Task.WhenAll(concurrentPayments);
            Assert.That(paymentsWaiting, Is.True, "Both payment requests must wait on the idempotency lock.");
            Assert.That(payments.Select(item => item.Id).Distinct().Count(), Is.EqualTo(1));
            var first = payments[0];
            var persistedPayment = await paymentContext.StudentFeePaymentTransactions.AsNoTracking().SingleAsync(item => item.Id == first.Id);
            Assert.Multiple(() =>
            {
                Assert.That(persistedPayment.StudentFeeInstallmentId, Is.EqualTo(installment.Id));
                Assert.That(persistedPayment.Amount, Is.EqualTo(firstRequest.Amount));
                Assert.That(persistedPayment.RecordedReference, Is.EqualTo(firstRequest.RecordedReference));
                Assert.That(persistedPayment.PaidDateTime!.Value.Ticks / TimeSpan.TicksPerMicrosecond,
                    Is.EqualTo(firstRequest.PaidAt.UtcDateTime.Ticks / TimeSpan.TicksPerMicrosecond));
            });
            var replay = await ledger.RecordOfflinePaymentAsync(installment.Id, scope.InstitutionId, 1, firstRequest, CancellationToken.None);
            var legacyBalance = await ledger.GetAdmissionBalanceAsync(admission.Id, scope.InstitutionId, CancellationToken.None);
            Assert.That(legacyBalance.Installments.Single().HeadAllocationComplete, Is.False);
            Assert.That(legacyBalance.Installments.Single().Heads, Is.Empty);
            var legacyReceipts = await ledger.GetReceiptsAsync(scope.InstitutionId,
                new StudentFeeReceiptListRequestDTO { StudentAdmissionId = admission.Id }, CancellationToken.None);
            Assert.That(legacyReceipts.Data.Single(item => item.PaymentTransactionId == replay.Id).HeadAllocationComplete, Is.False);
            Assert.That(replay.Id, Is.EqualTo(first.Id));
            var conflictingReplay = new StudentOfflinePaymentRequestDTO
            {
                IdempotencyKey = firstRequest.IdempotencyKey,
                Amount = firstRequest.Amount,
                PaidAt = firstRequest.PaidAt.AddTicks(TimeSpan.TicksPerMicrosecond),
                RecordedReference = firstRequest.RecordedReference
            };
            Assert.ThrowsAsync<ConflictException>(async () =>
                await ledger.RecordOfflinePaymentAsync(installment.Id, scope.InstitutionId, 1, conflictingReplay, CancellationToken.None));

            var partial = await ledger.GetAdmissionBalanceAsync(admission.Id, scope.InstitutionId, CancellationToken.None);
            Assert.Multiple(() =>
            {
                Assert.That(partial.TotalPaidAmount, Is.EqualTo(40m));
                Assert.That(partial.TotalOutstandingAmount, Is.EqualTo(60m));
            });

            var excess = new StudentOfflinePaymentRequestDTO
            {
                IdempotencyKey = $"excess-{Guid.NewGuid():N}",
                Amount = 61m,
                PaidAt = DateTimeOffset.UtcNow
            };
            Assert.ThrowsAsync<ValidationErrorException>(async () =>
                await ledger.RecordOfflinePaymentAsync(installment.Id, scope.InstitutionId, 1, excess, CancellationToken.None));

            await using var finalPaymentContext = new SkolenzaDbContext(options);
            var finalLedger = new StudentFeeLedgerRepository(finalPaymentContext);
            var final = await finalLedger.RecordOfflinePaymentAsync(installment.Id, scope.InstitutionId, 1,
                new StudentOfflinePaymentRequestDTO
                {
                    IdempotencyKey = $"final-{Guid.NewGuid():N}",
                    Amount = 60m,
                    PaidAt = DateTimeOffset.UtcNow
                }, CancellationToken.None);
            var balance = await finalLedger.GetAdmissionBalanceAsync(admission.Id, scope.InstitutionId, CancellationToken.None);
            var receipts = await finalLedger.GetReceiptsAsync(scope.InstitutionId,
                new StudentFeeReceiptListRequestDTO { StudentAdmissionId = admission.Id }, CancellationToken.None);
            Assert.Multiple(() =>
            {
                Assert.That(final.Id, Is.Not.EqualTo(first.Id));
                Assert.That(balance.TotalPaidAmount, Is.EqualTo(100m));
                Assert.That(balance.TotalOutstandingAmount, Is.Zero);
                Assert.That(receipts.TotalCount, Is.EqualTo(2));
                Assert.That(receipts.Data.Sum(item => item.Amount), Is.EqualTo(100m));
                Assert.That(receipts.Data.All(item => Math.Abs((DateTimeOffset.UtcNow - item.IssuedAt).TotalMinutes) < 1), Is.True);
            });

            var competingInstallment = new StudentFeeInstallment
            {
                InstitutionId = scope.InstitutionId,
                StudentAdmissionFeeQuoteId = quote.Id,
                InstallmentNumber = 2,
                DueDate = today,
                RecurringAmount = 100m,
                Status = (short)StudentFeeInstallmentStatus.Due
            };
            setup.StudentFeeInstallments.Add(competingInstallment);
            await setup.SaveChangesAsync();
            var competingApplications = new[] { $"fee-overpay-{Guid.NewGuid():N}", $"fee-overpay-{Guid.NewGuid():N}" };
            await using var installmentBlocker = new NpgsqlConnection(connection);
            await installmentBlocker.OpenAsync();
            await using var installmentBlockingTransaction = await installmentBlocker.BeginTransactionAsync();
            await using (var lockCommand = new NpgsqlCommand(
                "SELECT \"Id\" FROM skolenza.\"StudentFeeInstallment\" WHERE \"Id\" = @id FOR UPDATE",
                installmentBlocker, installmentBlockingTransaction))
            {
                lockCommand.Parameters.AddWithValue("id", competingInstallment.Id);
                Assert.That(await lockCommand.ExecuteScalarAsync(), Is.Not.Null);
            }

            async Task<bool> TryCompetingPaymentAsync(string applicationName)
            {
                var requestConnection = new NpgsqlConnectionStringBuilder(connection) { ApplicationName = applicationName };
                var requestOptions = new DbContextOptionsBuilder<SkolenzaDbContext>().UseNpgsql(requestConnection.ConnectionString).Options;
                await using var requestContext = new SkolenzaDbContext(requestOptions);
                try
                {
                    await new StudentFeeLedgerRepository(requestContext).RecordOfflinePaymentAsync(
                        competingInstallment.Id, scope.InstitutionId, 1,
                        new StudentOfflinePaymentRequestDTO
                        {
                            IdempotencyKey = applicationName,
                            Amount = 80m,
                            PaidAt = DateTimeOffset.UtcNow
                        }, CancellationToken.None);
                    return true;
                }
                catch (ValidationErrorException)
                {
                    return false;
                }
            }

            var competingPayments = competingApplications.Select(TryCompetingPaymentAsync).ToArray();
            var competingPaymentsWaiting = false;
            try
            {
                await using var monitor = new NpgsqlConnection(connection);
                await monitor.OpenAsync();
                var deadline = DateTime.UtcNow.AddSeconds(15);
                while (DateTime.UtcNow < deadline)
                {
                    await using var waiting = new NpgsqlCommand(
                        "SELECT count(*) FROM pg_stat_activity WHERE application_name = ANY(@names) AND wait_event_type = 'Lock'", monitor);
                    waiting.Parameters.AddWithValue("names", competingApplications);
                    if (Convert.ToInt32(await waiting.ExecuteScalarAsync()) == 2)
                    {
                        competingPaymentsWaiting = true;
                        break;
                    }
                    await Task.Delay(50);
                }
            }
            finally
            {
                await installmentBlockingTransaction.CommitAsync();
            }

            var competingResults = await Task.WhenAll(competingPayments);
            await using var competingVerification = new SkolenzaDbContext(options);
            var competingStored = await competingVerification.StudentFeeInstallments.AsNoTracking()
                .SingleAsync(item => item.Id == competingInstallment.Id);
            Assert.Multiple(() =>
            {
                Assert.That(competingPaymentsWaiting, Is.True);
                Assert.That(competingResults.Count(item => item), Is.EqualTo(1));
                Assert.That(competingStored.PaidAmount, Is.EqualTo(80m));
            });

            var gatewayInstallment = new StudentFeeInstallment
            {
                InstitutionId = scope.InstitutionId,
                StudentAdmissionFeeQuoteId = quote.Id,
                InstallmentNumber = 3,
                DueDate = today,
                RecurringAmount = 75m,
                Status = (short)StudentFeeInstallmentStatus.Due
            };
            setup.StudentFeeInstallments.Add(gatewayInstallment);
            await setup.SaveChangesAsync();
            await using var gatewayContext = new SkolenzaDbContext(options);
            var gatewayLedger = new StudentFeeLedgerRepository(gatewayContext);
            var gatewayCheckout = await gatewayLedger.PrepareCashfreeCheckoutAsync(gatewayInstallment.Id,
                scope.InstitutionId, $"gateway-{Guid.NewGuid():N}", CancellationToken.None);
            var gatewayPaidAt = DateTimeOffset.UtcNow;
            var gatewayPayment = new StudentCashfreePaymentDetailsDTO
            {
                OrderId = gatewayCheckout.GatewayOrderId,
                PaymentId = $"gateway-payment-{Guid.NewGuid():N}",
                Status = "SUCCESS",
                Amount = 75m,
                CurrencyCode = gatewayCheckout.CurrencyCode,
                PaidAt = gatewayPaidAt
            };
            var gatewayEventId = $"success-{Guid.NewGuid():N}";
            var webhookApplications = new[] { $"webhook-{Guid.NewGuid():N}", $"webhook-{Guid.NewGuid():N}" };
            await using var webhookBlocker = new NpgsqlConnection(connection);
            await webhookBlocker.OpenAsync();
            await using var webhookBlockingTransaction = await webhookBlocker.BeginTransactionAsync();
            await using (var lockCommand = new NpgsqlCommand(
                "SELECT \"Id\" FROM skolenza.\"StudentFeePaymentAttempt\" WHERE \"Id\" = @id FOR UPDATE",
                webhookBlocker, webhookBlockingTransaction))
            {
                lockCommand.Parameters.AddWithValue("id", gatewayCheckout.PaymentAttemptId);
                Assert.That(await lockCommand.ExecuteScalarAsync(), Is.Not.Null);
            }

            async Task<StudentCashfreePaymentStatusResponseDTO> ProcessWebhookAsync(string applicationName)
            {
                var requestConnection = new NpgsqlConnectionStringBuilder(connection) { ApplicationName = applicationName };
                var requestOptions = new DbContextOptionsBuilder<SkolenzaDbContext>().UseNpgsql(requestConnection.ConnectionString).Options;
                await using var requestContext = new SkolenzaDbContext(requestOptions);
                return await new StudentFeeLedgerRepository(requestContext).RecordCashfreePaymentAsync(
                    gatewayCheckout.PaymentAttemptId, gatewayPayment, gatewayEventId, "PAYMENT_SUCCESS", new string('b', 64), CancellationToken.None);
            }

            var concurrentWebhooks = webhookApplications.Select(ProcessWebhookAsync).ToArray();
            var webhooksWaiting = false;
            try
            {
                await using var monitor = new NpgsqlConnection(connection);
                await monitor.OpenAsync();
                var deadline = DateTime.UtcNow.AddSeconds(15);
                while (DateTime.UtcNow < deadline)
                {
                    await using var waiting = new NpgsqlCommand(
                        "SELECT count(*) FROM pg_stat_activity WHERE application_name = ANY(@names) AND wait_event_type = 'Lock'", monitor);
                    waiting.Parameters.AddWithValue("names", webhookApplications);
                    if (Convert.ToInt32(await waiting.ExecuteScalarAsync()) == 2)
                    {
                        webhooksWaiting = true;
                        break;
                    }
                    await Task.Delay(50);
                }
            }
            finally
            {
                await webhookBlockingTransaction.CommitAsync();
            }
            var webhookResults = await Task.WhenAll(concurrentWebhooks);
            Assert.That(webhooksWaiting, Is.True);
            Assert.That(webhookResults.Select(item => item.PaymentTransactionId).Distinct().Count(), Is.EqualTo(1));
            var gatewayStatus = webhookResults[0];
            var gatewayReplay = await gatewayLedger.RecordCashfreePaymentAsync(gatewayCheckout.PaymentAttemptId,
                gatewayPayment, gatewayEventId, "PAYMENT_SUCCESS", new string('b', 64), CancellationToken.None);
            Assert.Multiple(() =>
            {
                Assert.That(gatewayStatus.PaymentTransactionId, Is.Not.Null);
                Assert.That(gatewayReplay.PaymentTransactionId, Is.EqualTo(gatewayStatus.PaymentTransactionId));
                Assert.That(gatewayStatus.ReceiptNumber, Is.Not.Null.And.Not.Empty);
                Assert.That(gatewayStatus.PaidAt, Is.Not.Null);
                Assert.That(Math.Abs((gatewayStatus.PaidAt!.Value - gatewayPaidAt).TotalMilliseconds), Is.LessThan(1));
            });

            var studentId = stored.StudentId!.Value;
            var firstNewGuardian = new Guardian
            {
                InstitutionId = scope.InstitutionId,
                FirstName = "Synthetic",
                LastName = "Guardian A",
                IsActive = true,
                AddedById = 1,
                AddedDateTime = now
            };
            var secondNewGuardian = new Guardian
            {
                InstitutionId = scope.InstitutionId,
                FirstName = "Synthetic",
                LastName = "Guardian B",
                IsActive = true,
                AddedById = 1,
                AddedDateTime = now
            };
            setup.Guardians.AddRange(firstNewGuardian, secondNewGuardian);
            await setup.SaveChangesAsync();
            await using var linkContext = new SkolenzaDbContext(options);
            var linkRepository = new StudentRegistryRepository(linkContext, portal);
            var originalLink = (await linkRepository.GetStudentGuardiansAsync(
                scope.InstitutionId, studentId, false, CancellationToken.None)).Single();
            await linkRepository.UpdateGuardianLinkAsync(scope.InstitutionId, 1, studentId, originalLink.Id,
                new StudentGuardianLinkRequestDTO
                {
                    GuardianId = guardian.Id,
                    RelationshipType = StudentGuardianRelationshipType.Mother,
                    IsPrimary = false,
                    EffectiveFrom = today
                }, CancellationToken.None);
            var linkA = await linkRepository.LinkGuardianAsync(scope.InstitutionId, 1, studentId,
                new StudentGuardianLinkRequestDTO
                {
                    GuardianId = firstNewGuardian.Id,
                    RelationshipType = StudentGuardianRelationshipType.Guardian,
                    EffectiveFrom = today
                }, CancellationToken.None);
            var linkB = await linkRepository.LinkGuardianAsync(scope.InstitutionId, 1, studentId,
                new StudentGuardianLinkRequestDTO
                {
                    GuardianId = secondNewGuardian.Id,
                    RelationshipType = StudentGuardianRelationshipType.Guardian,
                    EffectiveFrom = today
                }, CancellationToken.None);
            var primaryApplications = new[] { $"primary-{Guid.NewGuid():N}", $"primary-{Guid.NewGuid():N}" };
            await using var primaryBlocker = new NpgsqlConnection(connection);
            await primaryBlocker.OpenAsync();
            await using var primaryBlockingTransaction = await primaryBlocker.BeginTransactionAsync();
            await using (var lockCommand = new NpgsqlCommand(
                "SELECT \"Id\" FROM skolenza.\"StudentGuardian\" WHERE \"Id\" = ANY(@ids) ORDER BY \"Id\" FOR UPDATE",
                primaryBlocker, primaryBlockingTransaction))
            {
                lockCommand.Parameters.AddWithValue("ids", new[] { linkA.Id, linkB.Id });
                await using var reader = await lockCommand.ExecuteReaderAsync();
                var count = 0;
                while (await reader.ReadAsync()) { count++; }
                Assert.That(count, Is.EqualTo(2));
            }

            async Task<bool> SetPrimaryConcurrentlyAsync(string applicationName, long linkId, long guardianId)
            {
                var requestConnection = new NpgsqlConnectionStringBuilder(connection) { ApplicationName = applicationName };
                var requestOptions = new DbContextOptionsBuilder<SkolenzaDbContext>().UseNpgsql(requestConnection.ConnectionString).Options;
                await using var requestContext = new SkolenzaDbContext(requestOptions);
                try
                {
                    await new StudentRegistryRepository(requestContext, portal).UpdateGuardianLinkAsync(
                        scope.InstitutionId, 1, studentId, linkId,
                        new StudentGuardianLinkRequestDTO
                        {
                            GuardianId = guardianId,
                            RelationshipType = StudentGuardianRelationshipType.Guardian,
                            IsPrimary = true,
                            EffectiveFrom = today
                        }, CancellationToken.None);
                    return true;
                }
                catch (ConflictException)
                {
                    return false;
                }
            }

            var primaryTasks = new[]
            {
                SetPrimaryConcurrentlyAsync(primaryApplications[0], linkA.Id, firstNewGuardian.Id),
                SetPrimaryConcurrentlyAsync(primaryApplications[1], linkB.Id, secondNewGuardian.Id)
            };
            var primaryWaiting = false;
            try
            {
                await using var monitor = new NpgsqlConnection(connection);
                await monitor.OpenAsync();
                var deadline = DateTime.UtcNow.AddSeconds(15);
                while (DateTime.UtcNow < deadline)
                {
                    await using var waiting = new NpgsqlCommand(
                        "SELECT count(*) FROM pg_stat_activity WHERE application_name = ANY(@names) AND wait_event_type = 'Lock'", monitor);
                    waiting.Parameters.AddWithValue("names", primaryApplications);
                    if (Convert.ToInt32(await waiting.ExecuteScalarAsync()) == 2)
                    {
                        primaryWaiting = true;
                        break;
                    }
                    await Task.Delay(50);
                }
            }
            finally
            {
                await primaryBlockingTransaction.CommitAsync();
            }

            var primaryResults = await Task.WhenAll(primaryTasks);
            Assert.That(primaryWaiting, Is.True);
            Assert.That(primaryResults.Count(item => item), Is.EqualTo(1));
            await using (var primaryVerification = new SkolenzaDbContext(options))
            {
                Assert.That(await primaryVerification.StudentGuardians.CountAsync(item =>
                    item.InstitutionId == scope.InstitutionId && item.StudentId == studentId && item.IsActive && item.IsPrimary), Is.EqualTo(1));
            }
            var identifierMaster = new StudentEducationIdentifierMaster
            {
                InstitutionId = scope.InstitutionId,
                CountryId = scope.CountryId,
                Code = $"TEST-{Guid.NewGuid():N}",
                Name = "Synthetic education identifier",
                IssuingAuthority = "Synthetic authority",
                IsActive = true,
                AddedById = 1,
                AddedDateTime = now
            };
            await using (var masterContext = new SkolenzaDbContext(options))
            {
                masterContext.StudentEducationIdentifierMasters.Add(identifierMaster);
                await masterContext.SaveChangesAsync();
            }
            await using var identifierContext = new SkolenzaDbContext(options);
            var encryption = new AesEncryptionService();
            var identifierKey = encryption.GenerateKey();
            var identifierRepository = new StudentEducationIdentifierRepository(identifierContext, encryption);
            var identifier = await identifierRepository.CreateAsync(scope.InstitutionId, 1,
                new StudentEducationIdentifierRequestDTO
                {
                    StudentId = studentId,
                    IdentifierMasterId = identifierMaster.Id,
                    Value = "SYNTHETIC123456"
                }, identifierKey, CancellationToken.None);
            var storedIdentifier = await identifierContext.StudentEducationIdentifiers.AsNoTracking()
                .SingleAsync(item => item.Id == identifier.Id);
            Assert.Multiple(() =>
            {
                Assert.That(identifier.MaskedValue, Is.EqualTo("***********3456"));
                Assert.That(storedIdentifier.EncryptedValue, Does.Not.Contain("SYNTHETIC123456"));
                Assert.That(encryption.Decrypt(storedIdentifier.EncryptedValue, identifierKey), Is.EqualTo("SYNTHETIC123456"));
                Assert.That(Math.Abs((DateTime.SpecifyKind(storedIdentifier.AddedDateTime, DateTimeKind.Utc) - DateTime.UtcNow).TotalMinutes), Is.LessThan(1));
            });
            var reviewedIdentifier = await identifierRepository.SetVerificationStatusAsync(scope.InstitutionId, 1, identifier.Id,
                (short)StudentEducationIdentifierVerificationStatus.Verified, null, CancellationToken.None);
            Assert.That(reviewedIdentifier.VerifiedDateTime, Is.Not.Null);
            Assert.That(Math.Abs((DateTime.SpecifyKind(reviewedIdentifier.VerifiedDateTime!.Value, DateTimeKind.Utc) - DateTime.UtcNow).TotalMinutes), Is.LessThan(1));
            var updatedIdentifier = await identifierRepository.UpdateAsync(scope.InstitutionId, 1, identifier.Id,
                new StudentEducationIdentifierUpdateDTO { Value = "SYNTHETIC987654" }, identifierKey, CancellationToken.None);
            Assert.That(updatedIdentifier.VerificationStatus, Is.EqualTo((short)StudentEducationIdentifierVerificationStatus.Unverified));
            Assert.That(await identifierContext.StudentEducationIdentifierVerificationHistories.CountAsync(
                item => item.StudentEducationIdentifierId == identifier.Id), Is.EqualTo(2));
            Assert.ThrowsAsync<NotFoundException>(async () => await identifierRepository.GetPageAsync(
                long.MaxValue, new StudentEducationIdentifierFilterDTO { StudentId = studentId }, CancellationToken.None));
            var activeEnrollment = await verification.StudentEnrollments.AsNoTracking()
                .SingleAsync(item => item.StudentId == studentId && item.IsActive);
            var exitDate = activeEnrollment.EffectiveFrom > today ? activeEnrollment.EffectiveFrom : today;
            var secondGuardian = new Guardian
            {
                InstitutionId = scope.InstitutionId,
                FirstName = "Synthetic",
                LastName = "Second Guardian",
                IsActive = true,
                AddedById = 1,
                AddedDateTime = now
            };
            setup.Guardians.Add(secondGuardian);
            await setup.SaveChangesAsync();
            var exitApplications = new[] { $"exit-{Guid.NewGuid():N}", $"exit-{Guid.NewGuid():N}" };
            await using var exitBlocker = new NpgsqlConnection(connection);
            await exitBlocker.OpenAsync();
            await using var exitBlockingTransaction = await exitBlocker.BeginTransactionAsync();
            await using (var lockCommand = new NpgsqlCommand(
                "SELECT \"Id\" FROM skolenza.\"Student\" WHERE \"Id\" = @id AND \"InstitutionId\" = @institutionId FOR UPDATE",
                exitBlocker, exitBlockingTransaction))
            {
                lockCommand.Parameters.AddWithValue("id", studentId);
                lockCommand.Parameters.AddWithValue("institutionId", scope.InstitutionId);
                Assert.That(await lockCommand.ExecuteScalarAsync(), Is.Not.Null);
            }

            async Task<bool> ExitConcurrentlyAsync(string applicationName)
            {
                var requestConnection = new NpgsqlConnectionStringBuilder(connection) { ApplicationName = applicationName };
                var requestOptions = new DbContextOptionsBuilder<SkolenzaDbContext>().UseNpgsql(requestConnection.ConnectionString).Options;
                await using var requestContext = new SkolenzaDbContext(requestOptions);
                try
                {
                    await new StudentRegistryRepository(requestContext, portal).ExitStudentAsync(
                        scope.InstitutionId, 1, studentId,
                        new StudentExitRequestDTO
                        {
                            EffectiveExitDate = exitDate,
                            ExitReason = StudentExitReason.Transfer,
                            PreviousSchoolName = "Synthetic previous school",
                            DestinationSchoolName = "Synthetic destination school"
                        },
                        CancellationToken.None);
                    return true;
                }
                catch (ConflictException)
                {
                    return false;
                }
            }

            var competingExits = exitApplications.Select(ExitConcurrentlyAsync).ToArray();
            var exitsWaiting = false;
            Task<bool>? competingLink = null;
            var linkWaiting = false;
            try
            {
                await using var monitor = new NpgsqlConnection(connection);
                await monitor.OpenAsync();
                var deadline = DateTime.UtcNow.AddSeconds(15);
                while (DateTime.UtcNow < deadline)
                {
                    await using var waiting = new NpgsqlCommand(
                        "SELECT count(*) FROM pg_stat_activity WHERE application_name = ANY(@names) AND wait_event_type = 'Lock'", monitor);
                    waiting.Parameters.AddWithValue("names", exitApplications);
                    if (Convert.ToInt32(await waiting.ExecuteScalarAsync()) == 2)
                    {
                        exitsWaiting = true;
                        break;
                    }
                    await Task.Delay(50);
                }
                if (exitsWaiting)
                {
                    var linkApplication = $"link-{Guid.NewGuid():N}";
                    async Task<bool> LinkDuringExitAsync()
                    {
                        var requestConnection = new NpgsqlConnectionStringBuilder(connection) { ApplicationName = linkApplication };
                        var requestOptions = new DbContextOptionsBuilder<SkolenzaDbContext>().UseNpgsql(requestConnection.ConnectionString).Options;
                        await using var requestContext = new SkolenzaDbContext(requestOptions);
                        try
                        {
                            await new StudentRegistryRepository(requestContext, portal).LinkGuardianAsync(
                                scope.InstitutionId, 1, studentId,
                                new StudentGuardianLinkRequestDTO
                                {
                                    GuardianId = secondGuardian.Id,
                                    RelationshipType = StudentGuardianRelationshipType.Guardian,
                                    EffectiveFrom = today
                                }, CancellationToken.None);
                            return true;
                        }
                        catch (KeyNotFoundException)
                        {
                            return false;
                        }
                    }
                    competingLink = LinkDuringExitAsync();
                    deadline = DateTime.UtcNow.AddSeconds(15);
                    while (DateTime.UtcNow < deadline)
                    {
                        await using var waiting = new NpgsqlCommand(
                            "SELECT count(*) FROM pg_stat_activity WHERE application_name = @name AND wait_event_type = 'Lock'", monitor);
                        waiting.Parameters.AddWithValue("name", linkApplication);
                        if (Convert.ToInt32(await waiting.ExecuteScalarAsync()) == 1)
                        {
                            linkWaiting = true;
                            break;
                        }
                        await Task.Delay(50);
                    }
                }
            }
            finally
            {
                await exitBlockingTransaction.CommitAsync();
            }

            var exitResults = await Task.WhenAll(competingExits);
            var linkedAfterExit = competingLink is not null && await competingLink;
            await using var exitVerification = new SkolenzaDbContext(options);
            var exitHistoryCount = await exitVerification.StudentExitHistories.CountAsync(item => item.StudentId == studentId);
            var enrollmentAfterExit = await exitVerification.StudentEnrollments.AsNoTracking()
                .SingleAsync(item => item.Id == activeEnrollment.Id);
            var exitRepository = new StudentRegistryRepository(exitVerification, portal);
            var exitHistory = await exitRepository.GetExitHistoryAsync(
                scope.InstitutionId, studentId, new StudentExitHistoryFilterDTO(), CancellationToken.None);
            var studentAfterExit = await exitVerification.Students.AsNoTracking().SingleAsync(item => item.Id == studentId);
            Assert.Multiple(() =>
            {
                Assert.That(exitsWaiting, Is.True);
                Assert.That(linkWaiting, Is.True);
                Assert.That(linkedAfterExit, Is.False);
                Assert.That(exitResults.Count(item => item), Is.EqualTo(1));
                Assert.That(exitHistoryCount, Is.EqualTo(1));
                Assert.That(enrollmentAfterExit.IsActive, Is.False);
                Assert.That(enrollmentAfterExit.EffectiveTo, Is.EqualTo(exitDate));
                Assert.That(studentAfterExit.LifecycleStatus, Is.EqualTo((short)StudentLifecycleStatus.Exited));
                Assert.That(studentAfterExit.IsActive, Is.False);
                Assert.That(exitHistory.TotalCount, Is.EqualTo(1));
                Assert.That(exitHistory.Data.Single().PreviousSchoolName, Is.EqualTo("Synthetic previous school"));
                Assert.That(exitHistory.Data.Single().DestinationSchoolName, Is.EqualTo("Synthetic destination school"));
            });
            Assert.ThrowsAsync<KeyNotFoundException>(async () => await exitRepository.GetExitHistoryAsync(
                long.MaxValue, studentId, new StudentExitHistoryFilterDTO(), CancellationToken.None));
        }
        if (scenario == 3)
        {
            Assert.That(await verification.StudentPortalOneTimeTokens.CountAsync(item => item.GuardianId == guardian.Id && item.InstitutionId == scope.InstitutionId), Is.EqualTo(1));
            var queued = await verification.EmailQueues.SingleAsync(item => item.InstitutionId == scope.InstitutionId && item.ToEmail == guardian.Email && item.TemplateCode == StudentConstants.PortalInvitationTemplateCode && item.IsSent == false);
            var envelope = JsonSerializer.Deserialize<Dictionary<string, string>>(queued.PlaceholdersJson!);
            Assert.That(envelope, Is.Not.Null);
            Assert.That(envelope!.Keys, Is.EquivalentTo(new[] { StudentConstants.PortalEmailProtectedPayloadKey }));
            Assert.That(queued.PlaceholdersJson, Does.Not.Contain("token="));
            var protector = (IStudentPortalEmailProtector)new StudentGatewaySecretProtector(new TestInstitutionKeyResolver());
            var decrypted = await protector.UnprotectAsync(scope.InstitutionId, envelope[StudentConstants.PortalEmailProtectedPayloadKey], CancellationToken.None);
            var placeholders = JsonSerializer.Deserialize<Dictionary<string, string>>(decrypted)!;
            var token = new Uri(placeholders["InvitationUrl"]).Query.Substring("?token=".Length);
            var storedTokenHash = await verification.StudentPortalOneTimeTokens.Where(item => item.GuardianId == guardian.Id).Select(item => item.TokenHash).SingleAsync();
            Assert.That(StudentPortalTokenHelper.HashToken(Uri.UnescapeDataString(token)), Is.EqualTo(storedTokenHash));

            var account = new StudentPortalAccount
            {
                InstitutionId = scope.InstitutionId,
                NormalizedEmail = guardian.Email!,
                PasswordHash = "synthetic-hash",
                SecurityStamp = Guid.NewGuid().ToString("N"),
                IsEmailVerified = true,
                IsActive = true,
                AddedDateTime = now
            };
            verification.StudentPortalAccounts.Add(account);
            await verification.SaveChangesAsync();
            verification.StudentPortalAccountGuardians.Add(new StudentPortalAccountGuardian
            {
                InstitutionId = scope.InstitutionId,
                StudentPortalAccountId = account.Id,
                GuardianId = guardian.Id,
                LinkedDateTime = now,
                IsActive = true
            });
            await verification.SaveChangesAsync();
            await using var resetContext = new SkolenzaDbContext(options);
            var resetRepository = new StudentPortalAuthRepository(resetContext, new EmailQueueRepository(resetContext), protector);
            await resetRepository.QueuePasswordResetAsync(scope.InstitutionId, account.NormalizedEmail, now, CancellationToken.None);

            await using var resetVerification = new SkolenzaDbContext(options);
            var resetQueue = await resetVerification.EmailQueues.SingleAsync(item => item.InstitutionId == scope.InstitutionId && item.ToEmail == guardian.Email && item.TemplateCode == StudentConstants.PortalPasswordResetTemplateCode);
            var resetEnvelope = JsonSerializer.Deserialize<Dictionary<string, string>>(resetQueue.PlaceholdersJson!);
            Assert.That(resetEnvelope!.Keys, Is.EquivalentTo(new[] { StudentConstants.PortalEmailProtectedPayloadKey }));
            Assert.That(resetQueue.PlaceholdersJson, Does.Not.Contain("token="));
            var resetPayload = await protector.UnprotectAsync(scope.InstitutionId, resetEnvelope[StudentConstants.PortalEmailProtectedPayloadKey], CancellationToken.None);
            var resetValues = JsonSerializer.Deserialize<Dictionary<string, string>>(resetPayload)!;
            var resetToken = new Uri(resetValues["PasswordResetUrl"]).Query.Substring("?token=".Length);
            var resetHash = await resetVerification.StudentPortalOneTimeTokens.Where(item => item.StudentPortalAccountId == account.Id).Select(item => item.TokenHash).SingleAsync();
            Assert.That(StudentPortalTokenHelper.HashToken(Uri.UnescapeDataString(resetToken)), Is.EqualTo(resetHash));

            var previousResetHash = StudentPortalTokenHelper.HashToken($"previous-{Guid.NewGuid():N}");
            resetVerification.StudentPortalOneTimeTokens.Add(new StudentPortalOneTimeToken
            {
                InstitutionId = scope.InstitutionId,
                StudentPortalAccountId = account.Id,
                Purpose = (short)StudentPortalOneTimeTokenPurpose.PasswordReset,
                TokenHash = previousResetHash,
                CreatedDateTime = now,
                ExpiresDateTime = now.AddMinutes(10)
            });
            await resetVerification.SaveChangesAsync();

            await resetRepository.ResetPasswordAsync(scope.InstitutionId, resetHash, "new-synthetic-hash", now, CancellationToken.None);
            Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
                await resetRepository.ResetPasswordAsync(scope.InstitutionId, previousResetHash, "stale-synthetic-hash", now, CancellationToken.None));

            await using var finalVerification = new SkolenzaDbContext(options);
            var updatedAccount = await finalVerification.StudentPortalAccounts.AsNoTracking().SingleAsync(item => item.Id == account.Id);
            Assert.That(updatedAccount.PasswordHash, Is.EqualTo("new-synthetic-hash"));

            var otherAccount = new StudentPortalAccount
            {
                InstitutionId = scope.InstitutionId,
                NormalizedEmail = $"OTHER-{Guid.NewGuid():N}@EXAMPLE.TEST",
                PasswordHash = "other-synthetic-hash",
                SecurityStamp = Guid.NewGuid().ToString("N"),
                IsEmailVerified = true,
                IsActive = true,
                AddedDateTime = now
            };
            resetVerification.StudentPortalAccounts.Add(otherAccount);
            await resetVerification.SaveChangesAsync();

            var competingHashes = new[]
            {
                StudentPortalTokenHelper.HashToken($"competing-a-{Guid.NewGuid():N}"),
                StudentPortalTokenHelper.HashToken($"competing-b-{Guid.NewGuid():N}")
            };
            var otherHash = StudentPortalTokenHelper.HashToken($"other-{Guid.NewGuid():N}");
            foreach (var hash in competingHashes.Append(otherHash))
            {
                resetVerification.StudentPortalOneTimeTokens.Add(new StudentPortalOneTimeToken
                {
                    InstitutionId = scope.InstitutionId,
                    StudentPortalAccountId = hash == otherHash ? otherAccount.Id : account.Id,
                    Purpose = (short)StudentPortalOneTimeTokenPurpose.PasswordReset,
                    TokenHash = hash,
                    CreatedDateTime = now,
                    ExpiresDateTime = now.AddMinutes(10)
                });
            }
            await resetVerification.SaveChangesAsync();

            async Task<bool> TryConcurrentResetAsync(string hash)
            {
                await using var context = new SkolenzaDbContext(options);
                var portal = new StudentPortalAuthRepository(context, new EmailQueueRepository(context), protector);
                try
                {
                    await portal.ResetPasswordAsync(scope.InstitutionId, hash, "concurrent-synthetic-hash", now, CancellationToken.None);
                    return true;
                }
                catch (UnauthorizedAccessException)
                {
                    return false;
                }
                catch (ConflictException)
                {
                    return false;
                }
            }

            var outcomes = await Task.WhenAll(competingHashes.Select(TryConcurrentResetAsync));
            Assert.That(outcomes.Count(success => success), Is.EqualTo(1));
            await using var isolationVerification = new SkolenzaDbContext(options);
            Assert.That(await isolationVerification.StudentPortalOneTimeTokens.AsNoTracking()
                .CountAsync(item => item.StudentPortalAccountId == account.Id &&
                    competingHashes.Contains(item.TokenHash) && item.ConsumedDateTime != null), Is.EqualTo(2));
            var unrelatedToken = await isolationVerification.StudentPortalOneTimeTokens.AsNoTracking()
                .SingleAsync(item => item.TokenHash == otherHash);
            Assert.That(unrelatedToken.ConsumedDateTime, Is.Null);

            var forcedHashes = new[]
            {
                StudentPortalTokenHelper.HashToken($"forced-a-{Guid.NewGuid():N}"),
                StudentPortalTokenHelper.HashToken($"forced-b-{Guid.NewGuid():N}")
            };
            foreach (var hash in forcedHashes)
            {
                resetVerification.StudentPortalOneTimeTokens.Add(new StudentPortalOneTimeToken
                {
                    InstitutionId = scope.InstitutionId,
                    StudentPortalAccountId = account.Id,
                    Purpose = (short)StudentPortalOneTimeTokenPurpose.PasswordReset,
                    TokenHash = hash,
                    CreatedDateTime = now,
                    ExpiresDateTime = now.AddMinutes(10)
                });
            }
            await resetVerification.SaveChangesAsync();

            var applicationNames = new[] { $"reset-test-{Guid.NewGuid():N}", $"reset-test-{Guid.NewGuid():N}" };
            await using var blocker = new NpgsqlConnection(connection);
            await blocker.OpenAsync();
            await using var blockingTransaction = await blocker.BeginTransactionAsync();
            await using (var command = new NpgsqlCommand(
                "SELECT \"Id\" FROM skolenza.\"StudentPortalAccount\" WHERE \"Id\" = @id AND \"InstitutionId\" = @institutionId FOR UPDATE",
                blocker, blockingTransaction))
            {
                command.Parameters.AddWithValue("id", account.Id);
                command.Parameters.AddWithValue("institutionId", scope.InstitutionId);
                Assert.That(await command.ExecuteScalarAsync(), Is.Not.Null);
            }

            async Task<(int Status, string Body)> ExecuteResetThroughMiddlewareAsync(string hash, string applicationName)
            {
                var requestConnection = new NpgsqlConnectionStringBuilder(connection) { ApplicationName = applicationName };
                var requestOptions = new DbContextOptionsBuilder<SkolenzaDbContext>()
                    .UseNpgsql(requestConnection.ConnectionString).Options;
                await using var requestDb = new SkolenzaDbContext(requestOptions);
                var portal = new StudentPortalAuthRepository(requestDb, new EmailQueueRepository(requestDb), protector);
                var http = new DefaultHttpContext();
                http.Response.Body = new MemoryStream();
                string? failureType = null;
                var middleware = new ErrorHandlerMiddleware(async context =>
                {
                    try
                    {
                        await portal.ResetPasswordAsync(scope.InstitutionId, hash, "forced-synthetic-hash", now, CancellationToken.None);
                    }
                    catch (Exception exception)
                    {
                        failureType = $"{exception.GetType().Name}/{exception.InnerException?.GetType().Name}";
                        throw;
                    }
                    context.Response.StatusCode = StatusCodes.Status200OK;
                }, NullLogger<ErrorHandlerMiddleware>.Instance);
                await middleware.InvokeAsync(http);
                if (http.Response.StatusCode == StatusCodes.Status500InternalServerError)
                {
                    TestContext.Progress.WriteLine($"Reset failure type: {failureType}");
                }
                http.Response.Body.Position = 0;
                using var reader = new StreamReader(http.Response.Body);
                return (http.Response.StatusCode, await reader.ReadToEndAsync());
            }

            var forcedRequests = forcedHashes.Select((hash, index) =>
                ExecuteResetThroughMiddlewareAsync(hash, applicationNames[index])).ToArray();
            var bothWaiting = false;
            try
            {
                await using var monitor = new NpgsqlConnection(connection);
                await monitor.OpenAsync();
                var deadline = DateTime.UtcNow.AddSeconds(15);
                while (DateTime.UtcNow < deadline)
                {
                    await using var waiting = new NpgsqlCommand(
                        "SELECT count(*) FROM pg_stat_activity WHERE application_name = ANY(@names) AND wait_event_type = 'Lock'",
                        monitor);
                    waiting.Parameters.AddWithValue("names", applicationNames);
                    if (Convert.ToInt32(await waiting.ExecuteScalarAsync()) == 2)
                    {
                        bothWaiting = true;
                        break;
                    }
                    await Task.Delay(50);
                }
            }
            finally
            {
                await blockingTransaction.CommitAsync();
            }

            var httpStatuses = await Task.WhenAll(forcedRequests);
            Assert.That(bothWaiting, Is.True, "Both reset requests must wait on the account lock before release.");
            Assert.That(httpStatuses.Select(result => result.Status),
                Is.EquivalentTo(new[] { StatusCodes.Status200OK, StatusCodes.Status409Conflict }));
            var conflictBody = httpStatuses.Single(result => result.Status == StatusCodes.Status409Conflict).Body;
            Assert.That(conflictBody, Does.Contain(StudentConstants.PortalPasswordResetConcurrencyMessage));
            Assert.That(conflictBody, Does.Not.Contain(forcedHashes[0]));
            Assert.That(conflictBody, Does.Not.Contain(forcedHashes[1]));
            Assert.That(conflictBody, Does.Not.Contain("40001"));

            var loginEmail = guardian.Email!.ToUpperInvariant();
            await isolationVerification.StudentPortalAccounts.Where(item => item.Id == account.Id)
                .ExecuteUpdateAsync(update => update
                    .SetProperty(item => item.NormalizedEmail, loginEmail)
                    .SetProperty(item => item.PasswordHash, "AQAAAA-synthetic-hash")
                    .SetProperty(item => item.FailedLoginCount, (short)0)
                    .SetProperty(item => item.LockoutEndDateTime, (DateTime?)null));
            var loginDomain = await isolationVerification.InstitutionPortalDomainMasters.AsNoTracking()
                .Where(item => item.InstitutionId == scope.InstitutionId && item.IsActive && !item.IsSoftDeleted)
                .Select(item => item.DomainName).SingleAsync();
            var passwords = DispatchProxy.Create<IPasswordService, BulkImportPermissionTests.TestProxy>();
            ((BulkImportPermissionTests.TestProxy)(object)passwords).InvokeMethod = (method, _) =>
                method.Name == nameof(IPasswordService.VerifyPassword) ? false : throw new AssertionException("Unexpected password call.");
            var tokens = DispatchProxy.Create<ITokenService, BulkImportPermissionTests.TestProxy>();
            ((BulkImportPermissionTests.TestProxy)(object)tokens).InvokeMethod = (_, _) =>
                throw new AssertionException("A failed login must not create a token.");
            for (var attempt = 0; attempt < StudentConstants.PortalLoginFailureLimit; attempt++)
            {
                await using var loginContext = new SkolenzaDbContext(options);
                var loginRepository = new StudentPortalAuthRepository(loginContext, new EmailQueueRepository(loginContext), protector);
                var handler = new StudentPortalLoginCommandHandler(loginRepository, passwords, tokens);
                var request = new StudentPortalLoginCommand(loginDomain, new StudentPortalLoginRequestDTO
                {
                    Email = loginEmail,
                    Password = "incorrect-synthetic-password"
                });
                Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await handler.Handle(request, CancellationToken.None));
            }

            var lockedAccount = await isolationVerification.StudentPortalAccounts.AsNoTracking().SingleAsync(item => item.Id == account.Id);
            Assert.That(lockedAccount.LockoutEndDateTime, Is.GreaterThan(DateTime.UtcNow));
            Assert.That(lockedAccount.FailedLoginCount, Is.EqualTo(0));

            var passwordService = new PasswordService(NullLogger<PasswordService>.Instance);
            const string syntheticPassword = "LocalTestPassword-123!";
            await isolationVerification.StudentPortalAccounts.Where(item => item.Id == account.Id)
                .ExecuteUpdateAsync(update => update
                    .SetProperty(item => item.PasswordHash, passwordService.HashPassword(syntheticPassword))
                    .SetProperty(item => item.FailedLoginCount, (short)0)
                    .SetProperty(item => item.LockoutEndDateTime, (DateTime?)null));

            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
            builder.Logging.ClearProviders();
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            const string signingKey = "synthetic-local-only-signing-key-at-least-32-bytes";
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JWTSettings:Secret"] = signingKey,
                ["JWTSettings:TokenLifetime"] = "00:15:00",
                ["JWTSettings:Issuer"] = "synthetic-local-issuer",
                ["JWTSettings:Audience"] = "synthetic-local-audience"
            });
            builder.Services.AddDbContext<SkolenzaDbContext>(settings => settings.UseNpgsql(connection));
            builder.Services.AddMediatR(settings => settings.RegisterServicesFromAssembly(typeof(StudentPortalLoginCommandHandler).Assembly));
            builder.Services.AddScoped<IStudentPortalAuthRepository, StudentPortalAuthRepository>();
            builder.Services.AddCors(settings => settings.AddPolicy(VerifiedPortalCorsPolicyProvider.FrontendPolicyName,
                policy => policy.WithOrigins("https://skolenza.vercel.app").AllowAnyHeader().AllowAnyMethod().AllowCredentials()));
            builder.Services.AddSingleton<ICorsPolicyProvider, VerifiedPortalCorsPolicyProvider>();
            builder.Services.AddScoped<IStudentFeeLedgerRepository, StudentFeeLedgerRepository>();
            builder.Services.AddScoped<IStudentPortalDocumentRepository, StudentPortalDocumentRepository>();
            var storedObjects = new HashSet<string>();
            var fileStorage = DispatchProxy.Create<IFileStorageService, BulkImportPermissionTests.TestProxy>();
            ((BulkImportPermissionTests.TestProxy)(object)fileStorage).InvokeMethod = (method, args) =>
            {
                switch (method.Name)
                {
                    case nameof(IFileStorageService.UploadFileAsync):
                        var key = $"synthetic/{Guid.NewGuid():N}";
                        lock (storedObjects)
                        {
                            storedObjects.Add(key);
                        }
                        return Task.FromResult(key);
                    case nameof(IFileStorageService.DeleteFileAsync):
                        lock (storedObjects)
                        {
                            return Task.FromResult(storedObjects.Remove((string)args![0]!));
                        }
                    case nameof(IFileStorageService.GetFileUrl):
                        lock (storedObjects)
                        {
                            Assert.That(storedObjects.Contains((string)args![0]!), Is.True);
                        }
                        return "https://example.test/synthetic-signed-download";
                    default:
                        throw new AssertionException($"Unexpected file-storage call: {method.Name}");
                }
            };
            builder.Services.AddSingleton(fileStorage);
            var gatewayConfig = DispatchProxy.Create<IStudentPaymentGatewayConfigurationRepository, BulkImportPermissionTests.TestProxy>();
            ((BulkImportPermissionTests.TestProxy)(object)gatewayConfig).InvokeMethod = (method, _) =>
                method.Name == nameof(IStudentPaymentGatewayConfigurationRepository.GetCredentialsAsync)
                    ? Task.FromResult<StudentPaymentGatewayConfiguration?>(new StudentPaymentGatewayConfiguration
                    {
                        Environment = (short)StudentPaymentGatewayEnvironment.Sandbox,
                        ClientId = "synthetic-client",
                        ProtectedClientSecret = "synthetic-secret"
                    })
                    : throw new AssertionException($"Unexpected gateway configuration call: {method.Name}");
            var checkoutProtector = DispatchProxy.Create<IStudentGatewaySecretProtector, BulkImportPermissionTests.TestProxy>();
            ((BulkImportPermissionTests.TestProxy)(object)checkoutProtector).InvokeMethod = (method, _) =>
                method.Name == nameof(IStudentGatewaySecretProtector.UnprotectAsync)
                    ? Task.FromResult("synthetic-decrypted-secret")
                    : throw new AssertionException($"Unexpected gateway protector call: {method.Name}");
            var providerCalls = 0;
            var cashfree = DispatchProxy.Create<IStudentCashfreeClient, BulkImportPermissionTests.TestProxy>();
            ((BulkImportPermissionTests.TestProxy)(object)cashfree).InvokeMethod = (method, _) =>
            {
                if (method.Name != nameof(IStudentCashfreeClient.CreateOrderAsync))
                {
                    throw new AssertionException($"Unexpected provider call: {method.Name}");
                }

                providerCalls++;
                return Task.FromResult(new StudentCashfreeProviderOrderDTO
                {
                    PaymentSessionId = "synthetic-payment-session",
                    ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10)
                });
            };
            builder.Services.AddSingleton(gatewayConfig);
            builder.Services.AddSingleton(checkoutProtector);
            builder.Services.AddSingleton(cashfree);
            builder.Services.AddScoped<IEmailQueueRepository, EmailQueueRepository>();
            builder.Services.AddSingleton<IStudentPortalEmailProtector>(protector);
            builder.Services.AddSingleton<IPasswordService>(passwordService);
            builder.Services.AddSingleton<IEncryptionService, AesEncryptionService>();
            var unitOfWork = DispatchProxy.Create<IUnitOfWork, BulkImportPermissionTests.TestProxy>();
            ((BulkImportPermissionTests.TestProxy)(object)unitOfWork).InvokeMethod = (method, _) =>
                throw new AssertionException($"Unexpected unit-of-work call: {method.Name}");
            builder.Services.AddSingleton(unitOfWork);
            builder.Services.AddScoped<ITokenService, TokenService>();
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(settings =>
            {
                settings.RequireHttpsMetadata = false;
                settings.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = "synthetic-local-issuer",
                    ValidAudience = "synthetic-local-audience",
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                    ClockSkew = TimeSpan.Zero
                };
            });
            builder.Services.AddAuthorization(settings => settings.AddPolicy(StudentConstants.PortalAuthenticationPolicy, policy => policy
                .RequireAuthenticatedUser()
                .RequireClaim(AppConstants.UserTypeClaim, StudentConstants.PortalUserType)
                .RequireClaim(StudentConstants.PortalAccountIdClaim)
                .RequireClaim("InstitutionId")
                .RequireClaim("TokenPurpose", AppConstants.AccessTokenPurpose)));
            builder.Services.AddControllers().AddApplicationPart(typeof(StudentPortalAuthController).Assembly);
            await using var app = builder.Build();
            app.UseMiddleware<ErrorHandlerMiddleware>();
            app.UseRouting();
            app.UseCors(VerifiedPortalCorsPolicyProvider.FrontendPolicyName);
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            await app.StartAsync();
            using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
            client.DefaultRequestHeaders.Host = "api.example.test";
            using (var preflight = new HttpRequestMessage(HttpMethod.Options, "/api/Student/portal-auth/login"))
            {
                preflight.Headers.TryAddWithoutValidation("Origin", $"https://{loginDomain}");
                preflight.Headers.TryAddWithoutValidation("Access-Control-Request-Method", "POST");
                using var preflightResponse = await client.SendAsync(preflight);
                Assert.That(preflightResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
                Assert.That(preflightResponse.Headers.GetValues("Access-Control-Allow-Origin").Single(),
                    Is.EqualTo($"https://{loginDomain}"));
            }

            using (var crossOriginLogin = await client.PostAsJsonAsync("/api/Student/portal-auth/login", new
            {
                portalHostname = loginDomain,
                email = loginEmail,
                password = syntheticPassword
            }))
            {
                Assert.That(crossOriginLogin.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            }

            client.DefaultRequestHeaders.Host = loginDomain;

            using var wrongLogin = await client.PostAsJsonAsync("/api/Student/portal-auth/login", new
            {
                email = loginEmail,
                password = "incorrect-local-test-password"
            });
            Assert.That(wrongLogin.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            using var login = await client.PostAsJsonAsync("/api/Student/portal-auth/login", new
            {
                email = loginEmail,
                password = syntheticPassword
            });
            Assert.That(login.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            using var loginPayload = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
            var loginData = loginPayload.RootElement.GetProperty("data");
            var accessToken = loginData.GetProperty("accessToken").GetString();
            var refreshToken = loginData.GetProperty("refreshToken").GetString();
            Assert.That(accessToken, Is.Not.Null.And.Not.Empty);
            Assert.That(refreshToken, Is.Not.Null.And.Not.Empty);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using var profile = await client.GetAsync("/api/Student/portal-auth/me");
            Assert.That(profile.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            await isolationVerification.StudentEnrollments.Where(item => item.StudentId == stored.StudentId && item.IsActive)
                .ExecuteUpdateAsync(update => update.SetProperty(item => item.EffectiveFrom, today));
            var documentRoot = $"/api/Student/portal/students/{stored.StudentId!.Value}/documents";
            using var requirementsResponse = await client.GetAsync($"{documentRoot}/requirements?personScope=1");
            Assert.That(requirementsResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            using var requirementsPayload = JsonDocument.Parse(await requirementsResponse.Content.ReadAsStringAsync());
            Assert.That(requirementsPayload.RootElement.GetProperty("data").EnumerateArray()
                .Any(item => item.GetProperty("id").GetInt64() == requirement.Id), Is.True);
            async Task<HttpResponseMessage> UploadDocumentAsync(string personScope = "1")
            {
                using var form = new MultipartFormDataContent();
                form.Add(new StringContent(requirement.Id.ToString()), "requirementId");
                form.Add(new StringContent(personScope), "personScope");
                var file = new ByteArrayContent(Encoding.UTF8.GetBytes("%PDF-1.4 synthetic test data"));
                file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
                form.Add(file, "file", "synthetic.pdf");
                return await client.PostAsync(documentRoot, form);
            }

            using var uploaded = await UploadDocumentAsync();
            Assert.That(uploaded.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            using var uploadedPayload = JsonDocument.Parse(await uploaded.Content.ReadAsStringAsync());
            var documentId = uploadedPayload.RootElement.GetProperty("data").GetProperty("id").GetInt64();
            Assert.That(uploadedPayload.RootElement.GetProperty("data").GetProperty("versionNumber").GetInt32(), Is.EqualTo(1));
            var storedSubmission = await isolationVerification.StudentRecordDocuments.AsNoTracking()
                .Where(item => item.Id == documentId).Select(item => item.SubmittedDateTime).SingleAsync();
            Assert.That(Math.Abs((DateTime.SpecifyKind(storedSubmission, DateTimeKind.Utc) - DateTime.UtcNow).TotalMinutes), Is.LessThan(2));
            await using (var documentContext = new SkolenzaDbContext(options))
            {
                var documentAuth = new StudentPortalAuthRepository(
                    documentContext, new EmailQueueRepository(documentContext), protector);
                var documentRepository = new StudentPortalDocumentRepository(documentContext, documentAuth);
                var documentPage = await documentRepository.GetPageAsync(
                    scope.InstitutionId, account.Id, stored.StudentId.Value,
                    new StudentPortalDocumentFilterDTO(), CancellationToken.None);
                Assert.That(documentPage.Data.Any(item => item.Id == documentId), Is.True);
            }
            using var documentList = await client.GetAsync(documentRoot);
            using var documentDownload = await client.GetAsync($"{documentRoot}/{documentId}/download");
            Assert.That(documentList.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(documentDownload.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            using var duplicateUpload = await UploadDocumentAsync();
            Assert.That(duplicateUpload.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
            using var guardianWithoutId = await UploadDocumentAsync("2");
            Assert.That(guardianWithoutId.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            using var wrongStudentDownload = await client.GetAsync($"/api/Student/portal/students/{long.MaxValue}/documents/{documentId}/download");
            Assert.That(wrongStudentDownload.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

            await isolationVerification.StudentPortalAccounts.Where(item => item.Id == account.Id)
                .ExecuteUpdateAsync(update => update.SetProperty(item => item.IsActive, false));
            using var disabledRequirements = await client.GetAsync($"{documentRoot}/requirements?personScope=1");
            using var disabledDownload = await client.GetAsync($"{documentRoot}/{documentId}/download");
            Assert.That(disabledRequirements.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(disabledDownload.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            await isolationVerification.StudentPortalAccounts.Where(item => item.Id == account.Id)
                .ExecuteUpdateAsync(update => update.SetProperty(item => item.IsActive, true));
            await isolationVerification.StudentPortalAccounts.Where(item => item.Id == account.Id)
                .ExecuteUpdateAsync(update => update.SetProperty(item => item.IsEmailVerified, false));
            using var unverifiedDownload = await client.GetAsync($"{documentRoot}/{documentId}/download");
            Assert.That(unverifiedDownload.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            await isolationVerification.StudentPortalAccounts.Where(item => item.Id == account.Id)
                .ExecuteUpdateAsync(update => update.SetProperty(item => item.IsEmailVerified, true));
            var reviewerId = await isolationVerification.StaffMembers.AsNoTracking()
                .Where(item => item.InstitutionId == scope.InstitutionId)
                .Select(item => item.Id).FirstAsync();
            var reviewApplications = new[] { $"student-review-{Guid.NewGuid():N}", $"student-review-{Guid.NewGuid():N}" };
            await using var reviewBlocker = new NpgsqlConnection(connection);
            await reviewBlocker.OpenAsync();
            await using var reviewBlockingTransaction = await reviewBlocker.BeginTransactionAsync();
            await using (var lockDocument = new NpgsqlCommand(
                "SELECT \"Id\" FROM skolenza.\"StudentRecordDocument\" WHERE \"Id\" = @id FOR UPDATE",
                reviewBlocker, reviewBlockingTransaction))
            {
                lockDocument.Parameters.AddWithValue("id", documentId);
                Assert.That(await lockDocument.ExecuteScalarAsync(), Is.Not.Null);
            }

            async Task<bool> ReviewConcurrentlyAsync(string applicationName)
            {
                var requestConnection = new NpgsqlConnectionStringBuilder(connection) { ApplicationName = applicationName };
                var requestOptions = new DbContextOptionsBuilder<SkolenzaDbContext>().UseNpgsql(requestConnection.ConnectionString).Options;
                await using var documentContext = new SkolenzaDbContext(requestOptions);
                var documentAuth = new StudentPortalAuthRepository(documentContext, new EmailQueueRepository(documentContext), protector);
                var documentRepository = new StudentPortalDocumentRepository(documentContext, documentAuth);
                try
                {
                    await documentRepository.ReviewAsync(scope.InstitutionId, reviewerId, stored.StudentId.Value,
                        documentId, new StudentAdmissionDocumentReviewDTO
                        {
                            Status = StudentDocumentReviewStatus.ReturnedForCorrection,
                            Reason = "Synthetic review"
                        }, CancellationToken.None);
                    return true;
                }
                catch (ConflictException)
                {
                    return false;
                }
            }

            var concurrentReviews = reviewApplications.Select(ReviewConcurrentlyAsync).ToArray();
            var reviewsWaiting = false;
            try
            {
                await using var monitor = new NpgsqlConnection(connection);
                await monitor.OpenAsync();
                var deadline = DateTime.UtcNow.AddSeconds(15);
                while (DateTime.UtcNow < deadline)
                {
                    await using var waiting = new NpgsqlCommand(
                        "SELECT count(*) FROM pg_stat_activity WHERE application_name = ANY(@names) AND wait_event_type = 'Lock'", monitor);
                    waiting.Parameters.AddWithValue("names", reviewApplications);
                    if (Convert.ToInt32(await waiting.ExecuteScalarAsync()) == 2)
                    {
                        reviewsWaiting = true;
                        break;
                    }

                    await Task.Delay(50);
                }
            }
            finally
            {
                await reviewBlockingTransaction.CommitAsync();
            }

            var reviewResults = await Task.WhenAll(concurrentReviews);
            Assert.That(reviewsWaiting, Is.True, "Both document reviews must contend for the document row.");
            Assert.That(reviewResults.Count(result => result), Is.EqualTo(1));
            Assert.That(await isolationVerification.StudentRecordDocumentReviewHistories.AsNoTracking()
                .CountAsync(item => item.StudentRecordDocumentId == documentId), Is.EqualTo(2));
            using var revisedUpload = await UploadDocumentAsync();
            Assert.That(revisedUpload.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            using var revisedPayload = JsonDocument.Parse(await revisedUpload.Content.ReadAsStringAsync());
            Assert.That(revisedPayload.RootElement.GetProperty("data").GetProperty("versionNumber").GetInt32(), Is.EqualTo(2));
            Assert.That(revisedPayload.RootElement.GetProperty("data").GetProperty("supersedesDocumentId").GetInt64(), Is.EqualTo(documentId));
            var revisedId = revisedPayload.RootElement.GetProperty("data").GetProperty("id").GetInt64();
            async Task ReturnDocumentAsync(long id)
            {
                await using var documentContext = new SkolenzaDbContext(options);
                var documentAuth = new StudentPortalAuthRepository(documentContext, new EmailQueueRepository(documentContext), protector);
                var repository = new StudentPortalDocumentRepository(documentContext, documentAuth);
                await repository.ReviewAsync(scope.InstitutionId, reviewerId, stored.StudentId.Value, id,
                    new StudentAdmissionDocumentReviewDTO
                    {
                        Status = StudentDocumentReviewStatus.ReturnedForCorrection,
                        Reason = "Synthetic follow-up"
                    }, CancellationToken.None);
            }

            await ReturnDocumentAsync(revisedId);
            var objectsBeforeRace = storedObjects.Count;
            await using (var studentBlocker = new NpgsqlConnection(connection))
            {
                await studentBlocker.OpenAsync();
                await using var studentLock = await studentBlocker.BeginTransactionAsync();
                await using (var lockStudent = new NpgsqlCommand(
                    "SELECT \"Id\" FROM skolenza.\"Student\" WHERE \"Id\" = @id FOR UPDATE", studentBlocker, studentLock))
                {
                    lockStudent.Parameters.AddWithValue("id", stored.StudentId.Value);
                    await lockStudent.ExecuteScalarAsync();
                }

                var uploads = new[] { UploadDocumentAsync(), UploadDocumentAsync() };
                var bothUploadsWaiting = false;
                try
                {
                    await using var monitor = new NpgsqlConnection(connection);
                    await monitor.OpenAsync();
                    var deadline = DateTime.UtcNow.AddSeconds(15);
                    while (DateTime.UtcNow < deadline)
                    {
                        await using var waiting = new NpgsqlCommand(
                            "SELECT count(*) FROM pg_stat_activity WHERE datname = current_database() AND wait_event_type = 'Lock' AND query LIKE '%Student%FOR UPDATE%'", monitor);
                        if (Convert.ToInt32(await waiting.ExecuteScalarAsync()) >= 2)
                        {
                            bothUploadsWaiting = true;
                            break;
                        }

                        await Task.Delay(50);
                    }
                }
                finally
                {
                    await studentLock.CommitAsync();
                }

                var uploadResults = await Task.WhenAll(uploads);
                Assert.That(bothUploadsWaiting, Is.True, "Both uploads must contend for the Student row.");
                Assert.That(uploadResults.Select(result => result.StatusCode),
                    Is.EquivalentTo(new[] { HttpStatusCode.OK, HttpStatusCode.Conflict }));
                foreach (var response in uploadResults)
                {
                    response.Dispose();
                }
            }
            Assert.That(storedObjects.Count, Is.EqualTo(objectsBeforeRace + 1));
            var latestDocumentId = await isolationVerification.StudentRecordDocuments.AsNoTracking()
                .Where(item => item.StudentId == stored.StudentId && item.RequirementId == requirement.Id)
                .OrderByDescending(item => item.VersionNumber).Select(item => item.Id).FirstAsync();
            await ReturnDocumentAsync(latestDocumentId);
            var objectsBeforeRevocation = storedObjects.Count;
            await using (var accountBlocker = new NpgsqlConnection(connection))
            {
                await accountBlocker.OpenAsync();
                await using var accountLock = await accountBlocker.BeginTransactionAsync();
                await using (var disableAccount = new NpgsqlCommand(
                    "UPDATE skolenza.\"StudentPortalAccount\" SET \"IsActive\" = false WHERE \"Id\" = @id", accountBlocker, accountLock))
                {
                    disableAccount.Parameters.AddWithValue("id", account.Id);
                    await disableAccount.ExecuteNonQueryAsync();
                }

                var upload = UploadDocumentAsync();
                var uploadWaiting = false;
                try
                {
                    await using var monitor = new NpgsqlConnection(connection);
                    await monitor.OpenAsync();
                    var deadline = DateTime.UtcNow.AddSeconds(15);
                    while (DateTime.UtcNow < deadline)
                    {
                        await using var waiting = new NpgsqlCommand(
                            "SELECT count(*) FROM pg_stat_activity WHERE datname = current_database() AND wait_event_type = 'Lock' AND query LIKE '%StudentPortalAccount%FOR SHARE%'", monitor);
                        if (Convert.ToInt32(await waiting.ExecuteScalarAsync()) > 0)
                        {
                            uploadWaiting = true;
                            break;
                        }

                        await Task.Delay(50);
                    }
                }
                finally
                {
                    await accountLock.CommitAsync();
                }

                using var revokedUpload = await upload;
                Assert.That(uploadWaiting, Is.True, "Upload must wait for account revocation.");
                Assert.That(revokedUpload.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            }
            Assert.That(storedObjects.Count, Is.EqualTo(objectsBeforeRevocation));
            await isolationVerification.StudentPortalAccounts.Where(item => item.Id == account.Id)
                .ExecuteUpdateAsync(update => update.SetProperty(item => item.IsActive, true));
            var guardianLinkId = await isolationVerification.StudentGuardians.AsNoTracking()
                .Where(item => item.StudentId == stored.StudentId && item.GuardianId == guardian.Id)
                .Select(item => item.Id)
                .SingleAsync();
            await using (var linkBlocker = new NpgsqlConnection(connection))
            {
                await linkBlocker.OpenAsync();
                await using var linkLock = await linkBlocker.BeginTransactionAsync();
                await using (var revokeLink = new NpgsqlCommand(
                    "UPDATE skolenza.\"StudentGuardian\" SET \"HasPortalAccess\" = false WHERE \"Id\" = @id", linkBlocker, linkLock))
                {
                    revokeLink.Parameters.AddWithValue("id", guardianLinkId);
                    await revokeLink.ExecuteNonQueryAsync();
                }

                var upload = UploadDocumentAsync();
                var uploadWaiting = false;
                try
                {
                    await using var monitor = new NpgsqlConnection(connection);
                    await monitor.OpenAsync();
                    var deadline = DateTime.UtcNow.AddSeconds(15);
                    while (DateTime.UtcNow < deadline)
                    {
                        await using var waiting = new NpgsqlCommand(
                            "SELECT count(*) FROM pg_stat_activity WHERE datname = current_database() AND wait_event_type = 'Lock' AND query LIKE '%StudentGuardian%FOR SHARE%'", monitor);
                        if (Convert.ToInt32(await waiting.ExecuteScalarAsync()) > 0)
                        {
                            uploadWaiting = true;
                            break;
                        }

                        await Task.Delay(50);
                    }
                }
                finally
                {
                    await linkLock.CommitAsync();
                }

                using var revokedUpload = await upload;
                Assert.That(uploadWaiting, Is.True, "Upload must wait for Guardian-link revocation.");
                Assert.That(revokedUpload.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            }
            Assert.That(storedObjects.Count, Is.EqualTo(objectsBeforeRevocation));
            await isolationVerification.StudentGuardians.Where(item => item.Id == guardianLinkId)
                .ExecuteUpdateAsync(update => update.SetProperty(item => item.HasPortalAccess, true));
            await using (var advisoryBlocker = new NpgsqlConnection(connection))
            await using (var competingLinkConnection = new NpgsqlConnection(connection))
            {
                await advisoryBlocker.OpenAsync();
                await competingLinkConnection.OpenAsync();
                await using var advisoryLock = await advisoryBlocker.BeginTransactionAsync();
                await using (var holdVersionLock = new NpgsqlCommand(
                    "SELECT pg_advisory_xact_lock(hashtextextended(@key, 0))", advisoryBlocker, advisoryLock))
                {
                    holdVersionLock.Parameters.AddWithValue("key", $"student-document:{scope.InstitutionId}:{stored.StudentId.Value}:{requirement.Id}:1:");
                    await holdVersionLock.ExecuteNonQueryAsync();
                }

                var upload = UploadDocumentAsync();
                var uploadWaiting = false;
                var revocationWaiting = false;
                var advisoryCommitted = false;
                Task<int>? revokeTask = null;
                try
                {
                    await using var monitor = new NpgsqlConnection(connection);
                    await monitor.OpenAsync();
                    var deadline = DateTime.UtcNow.AddSeconds(15);
                    while (DateTime.UtcNow < deadline)
                    {
                        await using var waiting = new NpgsqlCommand(
                            "SELECT count(*) FROM pg_stat_activity WHERE datname = current_database() AND wait_event_type = 'Lock' AND query LIKE '%pg_advisory_xact_lock%'", monitor);
                        if (Convert.ToInt32(await waiting.ExecuteScalarAsync()) > 0)
                        {
                            uploadWaiting = true;
                            break;
                        }
                        await Task.Delay(50);
                    }

                    await using var revokeLink = new NpgsqlCommand(
                        "UPDATE skolenza.\"StudentGuardian\" SET \"HasPortalAccess\" = false WHERE \"Id\" = @id", competingLinkConnection);
                    revokeLink.Parameters.AddWithValue("id", guardianLinkId);
                    revokeTask = revokeLink.ExecuteNonQueryAsync();
                    deadline = DateTime.UtcNow.AddSeconds(15);
                    while (DateTime.UtcNow < deadline)
                    {
                        await using var waiting = new NpgsqlCommand(
                            "SELECT count(*) FROM pg_stat_activity WHERE datname = current_database() AND wait_event_type = 'Lock' AND query LIKE 'UPDATE skolenza.%StudentGuardian%'", monitor);
                        if (Convert.ToInt32(await waiting.ExecuteScalarAsync()) > 0)
                        {
                            revocationWaiting = true;
                            break;
                        }
                        await Task.Delay(50);
                    }
                    await advisoryLock.CommitAsync();
                    advisoryCommitted = true;
                    using var acceptedUpload = await upload;
                    await revokeTask;
                    Assert.That(uploadWaiting, Is.True, "Upload must hold access locks before waiting for the version lock.");
                    Assert.That(revocationWaiting, Is.True, "Link revocation must wait for the authorized upload.");
                    Assert.That(acceptedUpload.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                }
                finally
                {
                    if (!advisoryCommitted)
                    {
                        await advisoryLock.RollbackAsync();
                    }
                }
            }
            using (var deniedAfterLinkRevocation = await UploadDocumentAsync())
            {
                Assert.That(deniedAfterLinkRevocation.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            }
            await isolationVerification.StudentGuardians.Where(item => item.Id == guardianLinkId)
                .ExecuteUpdateAsync(update => update.SetProperty(item => item.HasPortalAccess, true));
            var feeRoot = $"/api/Student/portal/students/{stored.StudentId!.Value}/fees/admissions";
            using var portalAdmissions = await client.GetAsync(feeRoot);
            Assert.That(portalAdmissions.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            using var admissionPayload = JsonDocument.Parse(await portalAdmissions.Content.ReadAsStringAsync());
            Assert.That(admissionPayload.RootElement.GetProperty("data").EnumerateArray()
                .Any(item => item.GetProperty("admissionId").GetInt64() == admission.Id), Is.True);
            using var portalQuote = await client.GetAsync($"{feeRoot}/{admission.Id}/quote");
            using var portalBalance = await client.GetAsync($"{feeRoot}/{admission.Id}/balance");
            using var portalReceipts = await client.GetAsync($"{feeRoot}/{admission.Id}/receipts");
            Assert.That(portalQuote.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(portalBalance.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(portalReceipts.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            using var foreignAdmission = await client.GetAsync($"{feeRoot}/{long.MaxValue}/balance");
            using var foreignStudent = await client.GetAsync($"/api/Student/portal/students/{long.MaxValue}/fees/admissions");
            Assert.That(foreignAdmission.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(foreignStudent.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            var portalInstallmentId = await isolationVerification.StudentFeeInstallments.AsNoTracking()
                .Where(item => item.StudentAdmissionFeeQuoteId == quote.Id)
                .Select(item => item.Id)
                .SingleAsync();
            var checkoutPath = $"{feeRoot}/{admission.Id}/installments/{portalInstallmentId}/cashfree-checkout";
            var checkoutRequest = new { idempotencyKey = $"portal-http-{Guid.NewGuid():N}" };
            using var portalCheckout = await client.PostAsJsonAsync(checkoutPath, checkoutRequest);
            Assert.That(portalCheckout.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            using var checkoutPayload = JsonDocument.Parse(await portalCheckout.Content.ReadAsStringAsync());
            Assert.That(checkoutPayload.RootElement.GetProperty("data").GetProperty("paymentSessionId").GetString(),
                Is.EqualTo("synthetic-payment-session"));
            using var portalCheckoutRetry = await client.PostAsJsonAsync(checkoutPath, checkoutRequest);
            Assert.That(portalCheckoutRetry.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            using var foreignInstallment = await client.PostAsJsonAsync(
                $"{feeRoot}/{admission.Id}/installments/{long.MaxValue}/cashfree-checkout", checkoutRequest);
            Assert.That(foreignInstallment.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(providerCalls, Is.EqualTo(1));
            await using (var revocationConnection = new NpgsqlConnection(connection))
            {
                await revocationConnection.OpenAsync();
                await using var revocation = await revocationConnection.BeginTransactionAsync();
                await using (var revoke = new NpgsqlCommand(
                    "UPDATE skolenza.\"StudentGuardian\" SET \"HasPortalAccess\" = false WHERE \"Id\" = @id",
                    revocationConnection, revocation))
                {
                    revoke.Parameters.AddWithValue("id", guardianLinkId);
                    await revoke.ExecuteNonQueryAsync();
                }

                var concurrentKey = $"portal-revocation-{Guid.NewGuid():N}";
                var concurrentCheckout = client.PostAsJsonAsync(checkoutPath, new { idempotencyKey = concurrentKey });
                var checkoutWaiting = false;
                try
                {
                    await using var monitor = new NpgsqlConnection(connection);
                    await monitor.OpenAsync();
                    var deadline = DateTime.UtcNow.AddSeconds(15);
                    while (DateTime.UtcNow < deadline)
                    {
                        await using var waiting = new NpgsqlCommand(
                            "SELECT count(*) FROM pg_stat_activity WHERE datname = current_database() AND wait_event_type = 'Lock' AND query LIKE '%StudentPortalAccountGuardian%'",
                            monitor);
                        if (Convert.ToInt32(await waiting.ExecuteScalarAsync()) > 0)
                        {
                            checkoutWaiting = true;
                            break;
                        }

                        await Task.Delay(50);
                    }
                }
                finally
                {
                    await revocation.CommitAsync();
                }

                using var deniedCheckout = await concurrentCheckout;
                Assert.That(checkoutWaiting, Is.True, "Checkout must wait for the Guardian-link revocation.");
                Assert.That(deniedCheckout.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
                Assert.That(providerCalls, Is.EqualTo(1));
                Assert.That(await isolationVerification.StudentFeePaymentAttempts.AsNoTracking()
                    .AnyAsync(item => item.IdempotencyKey == concurrentKey), Is.False);
                using var revokedReplay = await client.PostAsJsonAsync(checkoutPath, checkoutRequest);
                Assert.That(revokedReplay.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
                Assert.That(providerCalls, Is.EqualTo(1));
                await isolationVerification.StudentGuardians.Where(item => item.Id == guardianLinkId)
                    .ExecuteUpdateAsync(update => update.SetProperty(item => item.HasPortalAccess, true));
            }
            await using (var advisoryConnection = new NpgsqlConnection(connection))
            await using (var competingRevocationConnection = new NpgsqlConnection(connection))
            {
                await advisoryConnection.OpenAsync();
                await competingRevocationConnection.OpenAsync();
                await using var advisoryTransaction = await advisoryConnection.BeginTransactionAsync();
                var firstCheckoutKey = $"portal-before-revocation-{Guid.NewGuid():N}";
                await using (var lockKey = new NpgsqlCommand(
                    "SELECT pg_advisory_xact_lock(hashtextextended(@key, 0))",
                    advisoryConnection, advisoryTransaction))
                {
                    lockKey.Parameters.AddWithValue("key", $"{scope.InstitutionId}:{firstCheckoutKey}");
                    await lockKey.ExecuteNonQueryAsync();
                }

                var firstCheckout = client.PostAsJsonAsync(checkoutPath, new { idempotencyKey = firstCheckoutKey });
                var checkoutHasAuthorizationLocks = false;
                var revocationWaiting = false;
                var advisoryReleased = false;
                Task<int>? competingRevocation = null;
                try
                {
                    await using var monitor = new NpgsqlConnection(connection);
                    await monitor.OpenAsync();
                    var deadline = DateTime.UtcNow.AddSeconds(15);
                    while (DateTime.UtcNow < deadline)
                    {
                        await using var waiting = new NpgsqlCommand(
                            "SELECT count(*) FROM pg_stat_activity WHERE datname = current_database() AND wait_event_type = 'Lock' AND query LIKE '%pg_advisory_xact_lock%'",
                            monitor);
                        if (Convert.ToInt32(await waiting.ExecuteScalarAsync()) > 0)
                        {
                            checkoutHasAuthorizationLocks = true;
                            break;
                        }

                        await Task.Delay(50);
                    }

                    await using var revoke = new NpgsqlCommand(
                        "UPDATE skolenza.\"StudentGuardian\" SET \"HasPortalAccess\" = false WHERE \"Id\" = @id",
                        competingRevocationConnection);
                    revoke.Parameters.AddWithValue("id", guardianLinkId);
                    competingRevocation = revoke.ExecuteNonQueryAsync();
                    deadline = DateTime.UtcNow.AddSeconds(15);
                    while (DateTime.UtcNow < deadline)
                    {
                        await using var waiting = new NpgsqlCommand(
                            "SELECT count(*) FROM pg_stat_activity WHERE datname = current_database() AND wait_event_type = 'Lock' AND query LIKE 'UPDATE skolenza.%StudentGuardian%'",
                            monitor);
                        if (Convert.ToInt32(await waiting.ExecuteScalarAsync()) > 0)
                        {
                            revocationWaiting = true;
                            break;
                        }

                        await Task.Delay(50);
                    }

                    await advisoryTransaction.CommitAsync();
                    advisoryReleased = true;
                    using var acceptedCheckout = await firstCheckout;
                    Assert.That(acceptedCheckout.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                    Assert.That(await competingRevocation, Is.EqualTo(1));
                }
                finally
                {
                    if (!advisoryReleased)
                    {
                        await advisoryTransaction.CommitAsync();
                    }
                }

                Assert.That(checkoutHasAuthorizationLocks, Is.True);
                Assert.That(revocationWaiting, Is.True);
                Assert.That(providerCalls, Is.EqualTo(2));
                using var revokedAfterCheckout = await client.PostAsJsonAsync(checkoutPath, new { idempotencyKey = firstCheckoutKey });
                Assert.That(revokedAfterCheckout.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
                await isolationVerification.StudentGuardians.Where(item => item.Id == guardianLinkId)
                    .ExecuteUpdateAsync(update => update.SetProperty(item => item.HasPortalAccess, true));
            }
            using var refresh = await client.PostAsJsonAsync("/api/Student/portal-auth/refresh", new { refreshToken });
            Assert.That(refresh.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            using var refreshedPayload = JsonDocument.Parse(await refresh.Content.ReadAsStringAsync());
            var refreshedData = refreshedPayload.RootElement.GetProperty("data");
            var refreshedAccessToken = refreshedData.GetProperty("accessToken").GetString();
            var refreshedToken = refreshedData.GetProperty("refreshToken").GetString();
            Assert.That(refreshedAccessToken, Is.Not.Null.And.Not.Empty);
            Assert.That(refreshedToken, Is.Not.Null.And.Not.Empty);
            using var replay = await client.PostAsJsonAsync("/api/Student/portal-auth/refresh", new { refreshToken });
            Assert.That(replay.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", refreshedAccessToken);
            using var logout = await client.PostAsJsonAsync("/api/Student/portal-auth/logout", new { refreshToken = refreshedToken });
            Assert.That(logout.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            using var revoked = await client.PostAsJsonAsync("/api/Student/portal-auth/refresh", new { refreshToken = refreshedToken });
            Assert.That(revoked.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            client.DefaultRequestHeaders.Authorization = null;
            using var anonymousFees = await client.GetAsync(feeRoot);
            Assert.That(anonymousFees.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            using var anonymousProfile = await client.GetAsync("/api/Student/portal-auth/me");
            Assert.That(anonymousProfile.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            client.DefaultRequestHeaders.Host = "unverified.example.test";
            using var wrongDomain = await client.PostAsJsonAsync("/api/Student/portal-auth/login", new
            {
                email = loginEmail,
                password = syntheticPassword
            });
            Assert.That(wrongDomain.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            await app.StopAsync();
        }
    }

    private sealed class TestInstitutionKeyResolver : IInstitutionKeyResolver
    {
        public Task<string> ResolveAsync(long institutionId) => Task.FromResult("synthetic-disposable-test-key");
    }

}
