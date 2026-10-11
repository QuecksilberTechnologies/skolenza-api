using System.Reflection;
using skolenza.application.Common.Models.Security;
using skolenza.application.Constants;
using skolenza.application.DTOS.Staff.Bank;
using skolenza.application.Exceptions;
using skolenza.application.Features.StaffMemberCmd.BankInfo.Handlers;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("StaffMemberMutationPermission")]
public sealed class StaffMemberBankMutationPermissionTests
{
    [TestCase(false, false, true)]
    [TestCase(true, true, true)]
    [TestCase(true, false, false)]
    public void Bank_delete_rejects_foreign_scope_verified_or_locked_record(
        bool scopeAllowed, bool verified, bool editable)
    {
        var bank = new StaffMemberBankDetail
        {
            Id = 10, StaffMemberId = 25, IsInfoVerified = verified, IsEditAllowed = editable
        };
        var mutationCalled = false;
        var repository = Proxy<IStaffMemberBankRepository>((method, _) =>
        {
            if (method.Name == nameof(IStaffMemberBankRepository.GetSingleRecordAsync))
                return Task.FromResult(bank);
            mutationCalled = true;
            throw new InvalidOperationException("Unauthorized mutation reached repository.");
        });
        var unitOfWork = Proxy<IUnitOfWork>((method, _) => method.Name == "get_StaffMemberBankRepository"
            ? repository : throw new InvalidOperationException(method.Name));
        var common = Proxy<ICommonRequestService>((method, _) => method.Name switch
        {
            nameof(ICommonRequestService.ValidateInstitutionUserRequestAsync) => Task.FromResult(new CommonDecodedResult
            {
                Success = true, InstitutionId = 1, LoggedInStaffMemberId = 25, UserStaffMemberId = 25,
                RoleTypeId = ConstantValues.RoleTypeStaffMember
            }),
            nameof(ICommonRequestService.CanAccessStaffMemberDataAsync) => Task.FromResult(scopeAllowed),
            _ => throw new InvalidOperationException(method.Name)
        });
        var handler = new DeleteBankInfoQueryHandler(unitOfWork,
            NullLogger<DeleteBankInfoQueryHandler>.Instance, null!, common);

        Assert.ThrowsAsync<ForbiddenAccessException>(() => handler.Handle(
            new DeleteBankInfoQuery(new DeleteBankRequestDTO { Id = 10 }), CancellationToken.None));
        Assert.That(mutationCalled, Is.False);
        Assert.That(bank.IsSoftDeleted, Is.Not.True);
    }

    private static T Proxy<T>(Func<MethodInfo, object?[]?, object?> invoke) where T : class
    {
        var proxy = DispatchProxy.Create<T, BulkImportPermissionTests.TestProxy>();
        ((BulkImportPermissionTests.TestProxy)(object)proxy).InvokeMethod = invoke;
        return proxy;
    }
}
