using System.Reflection;
using skolenza.application.Common.Enums;
using skolenza.application.Common.Helpers;
using skolenza.application.Common.Models.Security;
using skolenza.application.DTOS.Common;
using skolenza.application.DTOS.Token;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEmail;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Interfaces.ITokenService;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("StaffMemberImportInvitations")]
public sealed class StaffMemberInvitationWorkflowTests
{
    [TestCase("sent", BulkImportInvitationStatus.Sent, 1)]
    [TestCase("failed", BulkImportInvitationStatus.Failed, 1)]
    [TestCase("uncertain", BulkImportInvitationStatus.DeliveryUnknown, 1)]
    [TestCase("token-failed", BulkImportInvitationStatus.Failed, 0)]
    public async Task Explicit_dispatch_generates_fresh_token_and_records_delivery_separately(
        string outcome, BulkImportInvitationStatus expected, int expectedEmailCalls)
    {
        var claims = 0;
        var emailCalls = 0;
        GetTokenInfoDTO? generated = null;
        BulkImportInvitationStatus? recorded = null;
        var actor = new CommonDecodedResult
        {
            Success = true, InstitutionId = 8, LoggedInStaffMemberId = 1, RoleId = 22,
            Claims = new TokenClaimsModel { InstitutionEncriptionKey = "test-key" }
        };
        var invitation = new StaffMemberImportInvitationDTO(2, Guid.NewGuid(), 321, "new@example.invalid", "Example StaffMember");
        var common = Proxy<ICommonRequestService>((_, _) => Task.FromResult(actor));
        var operation = Proxy<IOperationRepository>((_, _) => Task.FromResult<skolenza.domain.Entity.Operation?>(
            new skolenza.domain.Entity.Operation { IsActive = true, OperationType = (int)OperationType.Add }));
        var unit = Proxy<IUnitOfWork>((_, _) => operation);
        var repository = Proxy<IBulkImportRepository>((method, args) =>
        {
            if (method.Name == "ClaimStaffMemberInvitationAsync")
                return Task.FromResult<StaffMemberImportInvitationDTO?>(claims++ == 0 ? invitation : null);
            if (method.Name == "CompleteStaffMemberInvitationAsync")
            {
                recorded = (BulkImportInvitationStatus)args![3]!;
                return Task.CompletedTask;
            }
            if (method.Name == "ActAsync")
                return Task.FromResult(new BulkImportJobResponseDTO { CreatedCount = 1 });
            throw new AssertionException("Invitation dispatch must not insert accounts: " + method.Name);
        });
        var tokens = Proxy<ITokenService>((_, args) =>
        {
            generated = (GetTokenInfoDTO)args![0]!;
            Assert.That(claims, Is.EqualTo(1), "Generate only after a durable invitation claim.");
            return outcome == "token-failed" ? Task.FromException<string>(new Exception("test")) : Task.FromResult("temporary-token");
        });
        var emails = Proxy<IEmailService>((_, args) =>
        {
            emailCalls++;
            Assert.That(args![1], Is.EqualTo(invitation.Email));
            Assert.That(args[2], Is.EqualTo(actor.InstitutionId));
            return outcome == "uncertain" ? Task.FromException<bool>(new Exception("test SMTP failure")) : Task.FromResult(outcome == "sent");
        });
        var encoder = Proxy<IIdEncoderService>((_, _) => "encoded-test-id");
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            { ["FrontEndWebURL:BaseUrl"] = "https://ui.example.invalid" }).Build();
        var service = new BulkImportWorkflowService(new BulkImportPreviewService(unit, common), repository, common, unit,
            tokens, emails, encoder, config);
        await service.ActAsync(BulkImportMaster.StaffMember, BulkImportAction.SendInvitations,
            new BulkImportJobRequestDTO { JobId = Guid.NewGuid(), OperationId = 1 }, CancellationToken.None);
        Assert.That(recorded, Is.EqualTo(expected));
        Assert.That(emailCalls, Is.EqualTo(expectedEmailCalls));
        Assert.That(generated!.Expiry - generated.IssuedAt, Is.EqualTo(TimeSpan.FromMinutes(30)));
        Assert.That(generated.Email, Is.EqualTo(invitation.Email));
    }

    [Test]
    public void View_only_operation_cannot_send_invitations()
    {
        var actor = new CommonDecodedResult { Success = true, InstitutionId = 8, LoggedInStaffMemberId = 1, RoleId = 22 };
        var common = Proxy<ICommonRequestService>((_, _) => Task.FromResult(actor));
        var operation = Proxy<IOperationRepository>((_, _) => Task.FromResult<skolenza.domain.Entity.Operation?>(
            new skolenza.domain.Entity.Operation { IsActive = true, OperationType = (int)OperationType.View }));
        var unit = Proxy<IUnitOfWork>((_, _) => operation);
        var repository = Proxy<IBulkImportRepository>((_, _) => throw new AssertionException("View must not dispatch."));
        var workflow = new BulkImportWorkflowService(new BulkImportPreviewService(unit, common), repository, common, unit);
        Assert.ThrowsAsync<ForbiddenAccessException>(async () => await workflow.ActAsync(BulkImportMaster.StaffMember,
            BulkImportAction.SendInvitations, new BulkImportJobRequestDTO(), CancellationToken.None));
    }

    private static T Proxy<T>(Func<MethodInfo, object?[]?, object?> invoke) where T : class
    {
        var proxy = DispatchProxy.Create<T, BulkImportPermissionTests.TestProxy>();
        ((BulkImportPermissionTests.TestProxy)(object)proxy).InvokeMethod = invoke;
        return proxy;
    }
}
