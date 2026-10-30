using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using NUnit.Framework;
using skolenza.api.Controllers.Student;
using skolenza.application.Common.Enums;
using skolenza.application.Constants;
using skolenza.application.DTOS.Pagination;
using skolenza.application.DTOS.Student;
using skolenza.application.Exceptions;
using skolenza.application.Features.StudentPortalAuthCmd;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("StudentPortalFees")]
public sealed class StudentPortalFeeTests
{
    private static T Proxy<T>(Func<MethodInfo, object?[]?, object?> invoke) where T : class
    {
        var proxy = DispatchProxy.Create<T, BulkImportPermissionTests.TestProxy>();
        ((BulkImportPermissionTests.TestProxy)(object)proxy).InvokeMethod = invoke;
        return proxy;
    }

    private static StudentPortalProfileResponseDTO Profile(StudentPortalPersonaType type, long studentId)
    {
        return new StudentPortalProfileResponseDTO
        {
            InstitutionId = 1,
            PortalAccountId = 2,
            Personas = [new StudentPortalPersonaResponseDTO
            {
                Type = type,
                Id = 3,
                LinkedStudents = [new StudentPortalLinkedStudentResponseDTO { StudentId = studentId }]
            }]
        };
    }

    [TestCase("missing")]
    [TestCase("other-student")]
    [TestCase("student-persona")]
    public void Balance_rejects_accounts_without_a_linked_guardian(string scenario)
    {
        StudentPortalProfileResponseDTO? profile = scenario switch
        {
            "missing" => null,
            "other-student" => Profile(StudentPortalPersonaType.Guardian, 99),
            _ => Profile(StudentPortalPersonaType.Student, 4)
        };
        var auth = Proxy<IStudentPortalAuthRepository>((_, _) => Task.FromResult(profile));
        var fees = Proxy<IStudentFeeLedgerRepository>((_, _) => throw new AssertionException("Fee access must be denied before querying the ledger."));
        var handler = new GetStudentPortalFeeBalanceQueryHandler(auth, fees);

        Assert.ThrowsAsync<NotFoundException>(async () => await handler.Handle(
            new GetStudentPortalFeeBalanceQuery(1, 2, 4, 5), CancellationToken.None));
    }

    [Test]
    public void Guardian_cannot_read_another_students_admission()
    {
        var auth = Proxy<IStudentPortalAuthRepository>((_, _) => Task.FromResult<StudentPortalProfileResponseDTO?>(Profile(StudentPortalPersonaType.Guardian, 4)));
        var fees = Proxy<IStudentFeeLedgerRepository>((method, args) =>
        {
            Assert.That(method.Name, Is.EqualTo(nameof(IStudentFeeLedgerRepository.AdmissionBelongsToStudentAsync)));
            Assert.That(args![0..3], Is.EqualTo(new object[] { 1L, 4L, 99L }));
            return Task.FromResult(false);
        });
        var handler = new GetStudentPortalFeeQuoteQueryHandler(auth, fees);

        Assert.ThrowsAsync<NotFoundException>(async () => await handler.Handle(
            new GetStudentPortalFeeQuoteQuery(1, 2, 4, 99), CancellationToken.None));
    }

    [Test]
    public async Task Linked_guardian_receipts_are_forced_to_authorized_admission()
    {
        var auth = Proxy<IStudentPortalAuthRepository>((method, args) =>
        {
            Assert.That(args![0..2], Is.EqualTo(new object[] { 1L, 2L }));
            return Task.FromResult<StudentPortalProfileResponseDTO?>(Profile(StudentPortalPersonaType.Guardian, 4));
        });
        var fees = Proxy<IStudentFeeLedgerRepository>((method, args) => method.Name switch
        {
            nameof(IStudentFeeLedgerRepository.AdmissionBelongsToStudentAsync) => Task.FromResult(true),
            nameof(IStudentFeeLedgerRepository.GetReceiptsAsync) => Receipts(args),
            _ => throw new AssertionException($"Unexpected ledger call: {method.Name}")
        });
        static Task<PagedResponseDTO<StudentFeeReceiptResponseDTO>> Receipts(object?[]? args)
        {
            var filter = (StudentFeeReceiptListRequestDTO)args![1]!;
            Assert.Multiple(() =>
            {
                Assert.That(args[0], Is.EqualTo(1L));
                Assert.That(filter.StudentAdmissionId, Is.EqualTo(5L));
                Assert.That(filter.PageNumber, Is.EqualTo(2));
                Assert.That(filter.PageSize, Is.EqualTo(10));
            });
            return Task.FromResult(new PagedResponseDTO<StudentFeeReceiptResponseDTO>([], 11, 2, 10) { TotalPages = 2 });
        }

        var handler = new GetStudentPortalFeeReceiptsQueryHandler(auth, fees);
        var result = await handler.Handle(new GetStudentPortalFeeReceiptsQuery(
            1, 2, 4, 5, new StudentPortalFeeFilterDTO { PageNumber = 2 }), CancellationToken.None);
        Assert.That(result.PageNumber, Is.EqualTo(2));
        Assert.That(result.TotalRecords, Is.EqualTo(11));
    }

    [Test]
    public void Portal_fee_controller_requires_portal_policy()
    {
        var policy = typeof(StudentPortalFeesController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.That(policy?.Policy, Is.EqualTo(StudentConstants.PortalAuthenticationPolicy));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Checkout_checks_installment_ownership_before_accessing_gateway(bool belongs)
    {
        var auth = Proxy<IStudentPortalAuthRepository>((_, _) =>
            Task.FromResult<StudentPortalProfileResponseDTO?>(Profile(StudentPortalPersonaType.Guardian, 4)));
        var fees = Proxy<IStudentFeeLedgerRepository>((method, args) => method.Name switch
        {
            nameof(IStudentFeeLedgerRepository.AdmissionBelongsToStudentAsync) => Task.FromResult(true),
            nameof(IStudentFeeLedgerRepository.InstallmentBelongsToAdmissionAsync) => Task.FromResult(belongs),
            nameof(IStudentFeeLedgerRepository.PreparePortalCashfreeCheckoutAsync) => Task.FromResult(new StudentCashfreeCheckoutDataDTO
            {
                InstitutionId = 1,
                PaymentAttemptId = 9,
                StudentFeeInstallmentId = 6,
                GatewayOrderId = "synthetic-order",
                PaymentSessionId = "synthetic-session",
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5),
                CurrencyCode = "INR",
                Amount = 100
            }),
            _ => throw new AssertionException($"Unexpected ledger call: {method.Name}")
        });
        var configCalls = 0;
        var config = Proxy<IStudentPaymentGatewayConfigurationRepository>((_, _) =>
        {
            configCalls++;
            return Task.FromResult<StudentPaymentGatewayConfiguration?>(new StudentPaymentGatewayConfiguration
            {
                Environment = (short)StudentPaymentGatewayEnvironment.Sandbox
            });
        });
        var protector = Proxy<IStudentGatewaySecretProtector>((_, _) => throw new AssertionException("Existing checkout must not decrypt credentials."));
        var client = Proxy<IStudentCashfreeClient>((_, _) => throw new AssertionException("Existing checkout must not call the provider."));
        var handler = new CreateStudentPortalCashfreeCheckoutCommandHandler(auth, fees, config, protector, client);
        var command = new CreateStudentPortalCashfreeCheckoutCommand(1, 2, 4, 5, 6,
            new StudentPortalCashfreeCheckoutRequestDTO { IdempotencyKey = "synthetic-key" });

        if (!belongs)
        {
            Assert.ThrowsAsync<NotFoundException>(async () => await handler.Handle(command, CancellationToken.None));
            Assert.That(configCalls, Is.Zero);
            return;
        }

        var result = await handler.Handle(command, CancellationToken.None);
        Assert.That(result.Data.GatewayOrderId, Is.EqualTo("synthetic-order"));
        Assert.That(configCalls, Is.EqualTo(1));
    }
}
