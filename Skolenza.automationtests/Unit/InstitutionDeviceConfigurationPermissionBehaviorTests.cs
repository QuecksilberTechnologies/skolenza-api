using System.Reflection;
using skolenza.application.Common.Enums;
using skolenza.application.Common.Models.Security;
using skolenza.application.DTOS.Host;
using skolenza.application.DTOS.RoleModulePermission;
using skolenza.application.Exceptions;
using skolenza.application.Features.HostDeviceCmd;
using skolenza.application.Features.HostDeviceCmd.Handlers;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace skolenza.automationtests.Unit;

/// <summary>Guards the authorization boundary for the new Institution device-configuration actions.</summary>
[TestFixture]
public sealed class InstitutionDeviceConfigurationPermissionBehaviorTests
{
    private const int DeviceConfigurationModuleId = 49;
    private const int UpdateOperationId = 2;

    [Test]
    public async Task Typed_web_access_settings_require_the_institution_device_configuration_permission_before_handler_runs()
    {
        var calls = new PermissionCalls();
        var behavior = CreateBehavior<ApplyInstitutionDeviceSettingsCommand, ApiResponse<DeviceCommandSubmissionResponseDTO>>(
            calls,
            moduleCode: "INSTITUTION_DEVICE_CONFIGURATION",
            permissionResultCode: 1);
        var request = new ApplyInstitutionDeviceSettingsCommand(
            new UpdateInstitutionDeviceWebAccessRequestDTO
            {
                InstitutionDeviceId = "opaque-device-token",
                CurrentWebServerPassword = "test-only-password",
                LocalWebServerEnabled = true,
                ModuleId = DeviceConfigurationModuleId,
                OperationId = UpdateOperationId
            },
            InstitutionDeviceSettingsSection.WebAccess);
        var handlerWasCalled = false;

        var response = await behavior.Handle(
            request,
            _ =>
            {
                handlerWasCalled = true;
                return Task.FromResult(ApiResponse<DeviceCommandSubmissionResponseDTO>.Success(
                    new DeviceCommandSubmissionResponseDTO { Status = "Queued" }));
            },
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(handlerWasCalled, Is.True);
            Assert.That(response.IsSucceeded, Is.True);
            Assert.That(calls.ModuleId, Is.EqualTo(DeviceConfigurationModuleId));
            Assert.That(calls.OperationId, Is.EqualTo(UpdateOperationId));
            Assert.That(calls.PermissionCheckCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void New_manual_mqtts_dispatch_rejects_a_wrong_module_before_the_handler_runs()
    {
        var calls = new PermissionCalls();
        var behavior = CreateBehavior<DispatchInstitutionDeviceMqttsNowCommand, ApiResponse<ManualInstitutionDeviceCommandDispatchResponseDTO>>(
            calls,
            moduleCode: "STAFF_MEMBER",
            permissionResultCode: 1);
        var request = new DispatchInstitutionDeviceMqttsNowCommand(
            new DispatchInstitutionDeviceMqttsNowRequestDTO
            {
                InstitutionDeviceId = "opaque-device-token",
                ModuleId = DeviceConfigurationModuleId,
                OperationId = UpdateOperationId
            });
        var handlerWasCalled = false;

        Assert.ThrowsAsync<ForbiddenAccessException>(async () => await behavior.Handle(
            request,
            _ =>
            {
                handlerWasCalled = true;
                return Task.FromResult(ApiResponse<ManualInstitutionDeviceCommandDispatchResponseDTO>.Success(
                    new ManualInstitutionDeviceCommandDispatchResponseDTO()));
            },
            CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(handlerWasCalled, Is.False);
            Assert.That(calls.PermissionCheckCount, Is.EqualTo(0));
        });
    }

    [Test]
    public void New_gateway_replacement_rejects_a_denied_institution_operation_before_the_handler_runs()
    {
        var calls = new PermissionCalls();
        var behavior = CreateBehavior<ReplaceInstitutionDeviceHttpsGatewayUrlCommand, ApiResponse<DeviceCommandSubmissionResponseDTO>>(
            calls,
            moduleCode: "INSTITUTION_DEVICE_CONFIGURATION",
            permissionResultCode: 0);
        var request = new ReplaceInstitutionDeviceHttpsGatewayUrlCommand(
            new ReplaceInstitutionDeviceHttpsGatewayUrlRequestDTO
            {
                InstitutionDeviceId = "opaque-device-token",
                CurrentWebServerPassword = "test-only-password",
                ModuleId = DeviceConfigurationModuleId,
                OperationId = UpdateOperationId
            });
        var handlerWasCalled = false;

        Assert.ThrowsAsync<ForbiddenAccessException>(async () => await behavior.Handle(
            request,
            _ =>
            {
                handlerWasCalled = true;
                return Task.FromResult(ApiResponse<DeviceCommandSubmissionResponseDTO>.Success(
                    new DeviceCommandSubmissionResponseDTO()));
            },
            CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(handlerWasCalled, Is.False);
            Assert.That(calls.PermissionCheckCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void Institution_configuration_requests_do_not_expose_a_transport_selector()
    {
        Assert.Multiple(() =>
        {
            Assert.That(typeof(InstitutionDeviceSettingRequestDTO).GetProperty("Transport"), Is.Null);
            Assert.That(typeof(InstitutionDeviceSettingRequestDTO).GetProperty("CommandTransport"), Is.Null);
            Assert.That(typeof(InstitutionDeviceSettingRequestDTO).GetProperty("MqttTransport"), Is.Null);
            Assert.That(typeof(DispatchInstitutionDeviceMqttsNowRequestDTO).GetProperty("Transport"), Is.Null);
            Assert.That(typeof(DispatchInstitutionDeviceMqttsNowRequestDTO).GetProperty("CommandTransport"), Is.Null);
            Assert.That(typeof(DispatchInstitutionDeviceMqttsNowRequestDTO).GetProperty("MqttTransport"), Is.Null);
            Assert.That(typeof(ApplyInstitutionDeviceSettingsCommand).GetProperty("Transport"), Is.Null);
        });
    }

    [Test]
    public void Command_status_contract_has_retention_states_and_no_auto_deleted_state()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Enum.IsDefined(DeviceCommandStatus.Queued), Is.True);
            Assert.That(Enum.IsDefined(DeviceCommandStatus.Publishing), Is.True);
            Assert.That(Enum.IsDefined(DeviceCommandStatus.AwaitingResponse), Is.True);
            Assert.That(Enum.IsDefined(DeviceCommandStatus.Completed), Is.True);
            Assert.That(Enum.IsDefined(DeviceCommandStatus.RetryScheduled), Is.True);
            Assert.That(Enum.IsDefined(DeviceCommandStatus.Failed), Is.True);
            Assert.That(Enum.GetNames<DeviceCommandStatus>(), Does.Not.Contain("Deleted"));
        });
    }

    private static InstitutionDeviceConfigurationPermissionBehavior<TRequest, TResponse> CreateBehavior<TRequest, TResponse>(
        PermissionCalls calls,
        string moduleCode,
        int permissionResultCode)
        where TRequest : notnull
    {
        var storeProcedureRepository = CreateProxy<IStoreProcedureRepository>((method, args) =>
        {
            if (method.Name == nameof(IStoreProcedureRepository.CheckInstitutionStaffMemberPermissionAsync))
            {
                calls.PermissionCheckCount++;
                if (args is { Length: >= 5 })
                {
                    calls.ModuleId = args[3] is int moduleId ? moduleId : 0;
                    calls.OperationId = args[4] is int operationId ? operationId : 0;
                }
                return Task.FromResult(new InstitutionsUserPermissionCheckResponseDTO { ResultCode = permissionResultCode });
            }

            throw new NotSupportedException($"Unexpected repository call: {method.Name}");
        });
        var unitOfWork = CreateProxy<IUnitOfWork>((method, _) =>
            method.Name == "get_StoreProcedureRepository"
                ? storeProcedureRepository
                : throw new NotSupportedException($"Unexpected unit-of-work call: {method.Name}"));
        var commonRequestService = CreateProxy<ICommonRequestService>((method, args) => method.Name switch
        {
            nameof(ICommonRequestService.ValidateAuthenticatedRequestAsync) => Task.FromResult(new AuthenticatedRequestContext
            {
                UserType = LoginUserType.InstitutionStaffMember,
                AuthenticatedUserId = 44,
                InstitutionId = 9,
                RoleId = 7
            }),
            nameof(ICommonRequestService.GetModuleCodeAsync) => ResolveModuleCode(calls, args, moduleCode),
            nameof(ICommonRequestService.ValidateInstitutionUserRequestAsync) => Task.FromResult(new CommonDecodedResult
            {
                Success = true,
                InstitutionId = 9,
                LoggedInStaffMemberId = 44,
                RoleId = 7
            }),
            _ => throw new NotSupportedException($"Unexpected common-request call: {method.Name}")
        });

        return new InstitutionDeviceConfigurationPermissionBehavior<TRequest, TResponse>(
            unitOfWork,
            commonRequestService,
            NullLogger<InstitutionDeviceConfigurationPermissionBehavior<TRequest, TResponse>>.Instance);
    }

    private static Task<string?> ResolveModuleCode(PermissionCalls calls, object?[]? args, string moduleCode)
    {
        if (args is { Length: > 0 } && args[0] is int moduleId)
        {
            calls.ModuleId = moduleId;
        }

        return Task.FromResult<string?>(moduleCode);
    }

    private sealed class PermissionCalls
    {
        public int ModuleId { get; set; }
        public int OperationId { get; set; }
        public int PermissionCheckCount { get; set; }
    }

    private class InterfaceProxy : DispatchProxy
    {
        public Func<MethodInfo, object?[]?, object?> Handler { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            Handler(targetMethod ?? throw new InvalidOperationException("A proxy method was not supplied."), args);
    }

    private static T CreateProxy<T>(Func<MethodInfo, object?[]?, object?> handler)
        where T : class
    {
        var proxy = DispatchProxy.Create<T, InterfaceProxy>();
        ((InterfaceProxy)(object)proxy).Handler = handler;
        return proxy;
    }
}
