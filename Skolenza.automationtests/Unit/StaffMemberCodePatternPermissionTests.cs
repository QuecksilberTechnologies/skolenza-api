using System.Reflection;
using skolenza.application.Common.Enums;
using skolenza.application.Common.Models.Security;
using skolenza.application.DTOS.RoleModulePermission;
using skolenza.application.Common.Helpers;
using skolenza.application.DTOS.Institution;
using skolenza.application.Exceptions;
using skolenza.application.Features.InstitutionConfigurationCmd;
using skolenza.application.Features.InstitutionConfigurationCmd.Configuration.StaffCodeCmd.Handlers;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IRepositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("StaffMemberBulkCode")]
public sealed class StaffMemberCodePatternPermissionTests
{
    [TestCase(LoginUserType.Host, "HOST_CAMPUS_LIST", true)]
    [TestCase(LoginUserType.Host, "INSTITUTION_CAMPUSES", false)]
    [TestCase(LoginUserType.InstitutionStaffMember, "INSTITUTION_CAMPUSES", true)]
    [TestCase(LoginUserType.InstitutionStaffMember, "HOST_CAMPUS_LIST", false)]
    public async Task Location_module_binding_separates_host_and_institution(
        LoginUserType userType, string moduleCode, bool allowed)
    {
        var common = Proxy<ICommonRequestService>((_, _) => Task.FromResult<string?>(moduleCode));
        var unit = Proxy<IUnitOfWork>((_, _) => throw new AssertionException("Binding must not query permissions."));
        var behavior = new InstitutionLocationPermissionBehavior<skolenza.application.Features.InstitutionConfigurationCmd.Handlers.GetCampussQuery, bool>(
            unit, common, NullLogger<InstitutionLocationPermissionBehavior<skolenza.application.Features.InstitutionConfigurationCmd.Handlers.GetCampussQuery, bool>>.Instance);
        var method = behavior.GetType().GetMethod("EnsureExpectedModuleCodeAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var task = (Task)method.Invoke(behavior, new object[]
        {
            new skolenza.application.DTOs.BaseDTO.PermissionRequestDTO { ModuleId = 78, OperationId = 4 },
            CancellationToken.None, userType
        })!;
        if (allowed)
        {
            await task;
        }
        else
        {
            Assert.ThrowsAsync<ForbiddenAccessException>(async () => await task);
        }
    }

    [Test]
    public void Institution_permission_denial_is_forbidden_while_invalid_permission_context_is_unauthorized()
    {
        Assert.Throws<ForbiddenAccessException>(() => InstitutionRuntimePermissionValidator.EnsureAllowed(
            new InstitutionsUserPermissionCheckResponseDTO { ResultCode = 0 }));
        Assert.Throws<UnauthorizedAccessException>(() => InstitutionRuntimePermissionValidator.EnsureAllowed(
            new InstitutionsUserPermissionCheckResponseDTO { ResultCode = -1 }));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void Host_aggregate_cannot_bypass_pattern_preview(bool change)
    {
        var pattern = new skolenza.domain.Entity.StaffMemberCodePattern
            { Prefix = "EMP", Separator = "-", RunningNumberLength = 4 };
        var dto = new skolenza.application.DTOs.Institution.NewInstitutionStaffMemberCodePatternUpdateRequestDTO
            { Prefix = change ? "NEW" : "EMP" };
        var method = typeof(skolenza.application.Features.InstitutionManagementCmd.Commands.UpdateNewInstitutionCommandHandler)
            .GetMethod("ApplyStaffMemberCodePattern", BindingFlags.NonPublic | BindingFlags.Static)!;
        if (change)
        {
            var error = Assert.Throws<TargetInvocationException>(() => method.Invoke(null, new object[] { dto, pattern }));
            Assert.That(error!.InnerException, Is.TypeOf<ConflictException>());
        }
        else
        {
            Assert.That(method.Invoke(null, new object[] { dto, pattern }), Is.False);
        }
        Assert.That(pattern.Prefix, Is.EqualTo("EMP"));
    }

    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public async Task Existing_pipeline_rejects_wrong_module_or_denied_grant_before_handler(bool correctModule, bool granted)
    {
        var common = Proxy<ICommonRequestService>((method, _) => method.Name switch
        {
            "ValidateAuthenticatedRequestAsync" => Task.FromResult(new AuthenticatedRequestContext { UserType = LoginUserType.InstitutionStaffMember }),
            "ValidateInstitutionUserRequestAsync" => Task.FromResult(new CommonDecodedResult { Success = true, InstitutionId = 8, LoggedInStaffMemberId = 1, RoleId = 22 }),
            "GetModuleCodeAsync" => Task.FromResult<string?>(correctModule ? "INSTITUTION_STAFF_CODE" : "INSTITUTION_CAMPUSES"),
            _ => throw new InvalidOperationException(method.Name)
        });
        var stored = Proxy<IStoreProcedureRepository>((_, _) => Task.FromResult(new InstitutionsUserPermissionCheckResponseDTO { ResultCode = granted ? 1 : 0 }));
        var unit = Proxy<IUnitOfWork>((_, _) => stored);
        var behavior = new InstitutionLocationPermissionBehavior<UpdateStaffMemberCodePatternCommand, bool>(
            unit, common, NullLogger<InstitutionLocationPermissionBehavior<UpdateStaffMemberCodePatternCommand, bool>>.Instance);
        var invoked = false;
        Task<bool> Next(CancellationToken _)
        {
            invoked = true;
            return Task.FromResult(true);
        }

        var command = new UpdateStaffMemberCodePatternCommand(new SaveStaffMemberCodePatternRequestDTO { ModuleId = 29, OperationId = 2 });
        if (correctModule && granted)
        {
            Assert.That(await behavior.Handle(command, Next, CancellationToken.None), Is.True);
        }
        else
        {
            Assert.ThrowsAsync<ForbiddenAccessException>(async () => await behavior.Handle(command, Next, CancellationToken.None));
        }
        Assert.That(invoked, Is.EqualTo(correctModule && granted));
    }

    [TestCase("AddStaffMemberCodePattern", "add-staff-member-code-pattern", typeof(HttpPostAttribute))]
    [TestCase("UpdateStaffMemberCodePattern", "update-staff-member-code-pattern", typeof(HttpPutAttribute))]
    public void Pattern_routes_require_authentication(string method, string route, Type verb)
    {
        var info = typeof(skolenza.api.Controllers.Institution.InstitutionController).GetMethod(method)!;
        Assert.That(info.GetCustomAttribute<AuthorizeAttribute>(), Is.Not.Null);
        var attribute = info.GetCustomAttributes().Single(item => item.GetType() == verb);
        Assert.That(((Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute)attribute).Template, Is.EqualTo(route));
    }

    private static T Proxy<T>(Func<MethodInfo, object?[]?, object?> invoke) where T : class
    {
        var proxy = DispatchProxy.Create<T, BulkImportPermissionTests.TestProxy>();
        ((BulkImportPermissionTests.TestProxy)(object)proxy).InvokeMethod = invoke;
        return proxy;
    }

    [Test]
    public void Host_cannot_use_institution_pattern_write()
    {
        var common = Proxy<ICommonRequestService>((_, _) =>
            Task.FromResult(new AuthenticatedRequestContext { UserType = LoginUserType.Host }));
        var unit = Proxy<IUnitOfWork>((_, _) => throw new AssertionException("Institution grants must not be queried for Host."));
        var behavior = new InstitutionLocationPermissionBehavior<UpdateStaffMemberCodePatternCommand, bool>(
            unit, common, NullLogger<InstitutionLocationPermissionBehavior<UpdateStaffMemberCodePatternCommand, bool>>.Instance);
        Assert.ThrowsAsync<ForbiddenAccessException>(async () => await behavior.Handle(
            new UpdateStaffMemberCodePatternCommand(new SaveStaffMemberCodePatternRequestDTO()),
            _ => throw new AssertionException("Host must not reach the institution write handler."), CancellationToken.None));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void View_permission_cannot_create_or_update_pattern(bool create)
    {
        var common = Proxy<ICommonRequestService>((_, _) => Task.FromResult(
            new CommonDecodedResult { Success = true, InstitutionId = 8, LoggedInStaffMemberId = 1, RoleId = 22 }));
        var operations = Proxy<IOperationRepository>((_, _) => Task.FromResult<skolenza.domain.Entity.Operation?>(
            new skolenza.domain.Entity.Operation { IsActive = true, OperationType = (int)OperationType.View }));
        var unit = Proxy<IUnitOfWork>((method, _) => method.Name == "get_OperationRepository"
            ? operations : throw new AssertionException("View permission must not reach persistence."));
        var handler = new SaveStaffMemberCodePatternCommandHandler(unit, common);
        var dto = new SaveStaffMemberCodePatternRequestDTO { OperationId = 1 };
        Assert.ThrowsAsync<ForbiddenAccessException>(async () =>
        {
            if (create)
            {
                await handler.Handle(new CreateStaffMemberCodePatternCommand(dto), CancellationToken.None);
            }
            else
            {
                await handler.Handle(new UpdateStaffMemberCodePatternCommand(dto), CancellationToken.None);
            }
        });
    }
}

