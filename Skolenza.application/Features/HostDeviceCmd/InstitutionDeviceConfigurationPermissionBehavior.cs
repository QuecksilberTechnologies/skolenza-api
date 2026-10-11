// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Applies the established module-code and current role-permission
//           behavior to initial and runtime Institution device configuration.
// ================================================================

using System.Reflection;
using skolenza.application.Common.Enums;
using skolenza.application.Common.Helpers;
using skolenza.application.Constants;
using skolenza.application.DTOs.BaseDTO;
using skolenza.application.Exceptions;
using skolenza.application.Features.HostDeviceCmd.Handlers;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.HostDeviceCmd;

/// <summary>
/// Applies the same trusted module-code and database-backed permission contract
/// used by StaffMember feature requests to the device configuration request set.
/// </summary>
/// <typeparam name="TRequest">The MediatR command or query.</typeparam>
/// <typeparam name="TResponse">The MediatR response.</typeparam>
public sealed class InstitutionDeviceConfigurationPermissionBehavior<TRequest, TResponse>(
    IUnitOfWork unitOfWork,
    ICommonRequestService commonRequestService,
    ILogger<InstitutionDeviceConfigurationPermissionBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const string HostInitialDeviceConfigurationModuleCode = "HOST_INITIAL_DEVICE_CONFIGURATION";
    private const string InstitutionDeviceConfigurationModuleCode = "INSTITUTION_DEVICE_CONFIGURATION";
    private const string HostInstitutionCardInventoryModuleCode = "HOST_INSTITUTION_CARD_INVENTORY";
    private const string HostDeviceConfigurationModuleCode = "INSTITUTION_DEVICE_CONFIG";

    #region Permission Pipeline

    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (typeof(TRequest).Namespace?.StartsWith(
                "skolenza.application.Features.HostDeviceCmd",
                StringComparison.Ordinal) != true)
        {
            return await next();
        }

        var expectedModuleCode = ResolveExpectedModuleCode();
        if (expectedModuleCode is null)
        {
            return await next();
        }

        var principal = await commonRequestService.ValidateAuthenticatedRequestAsync();
        var permissionRequest = ResolvePermissionRequest(request)
            ?? throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);

        if (principal.UserType == LoginUserType.Host)
        {
            await HostRuntimePermissionValidator.ValidateAsync(
                commonRequestService,
                unitOfWork.StoreProcedureRepository,
                permissionRequest.ModuleId,
                permissionRequest.OperationId,
                cancellationToken);

            // Host read access uses the Host leaf module and its persisted grants.
            // Runtime changes remain governed by the Institution ownership boundary.
            if (!IsHostAllowedRequest())
            {
                throw new ForbiddenAccessException(AppConstants.ErrorMessages.PermissionDenied);
            }

            // Every Host role must submit the expected active module and pass
            // its persisted module-operation permission.
            await EnsureExpectedModuleCodeAsync(permissionRequest,
                IsConfigurationReadRequest() ? HostDeviceConfigurationModuleCode : expectedModuleCode);

            return await next();
        }

        await EnsureExpectedModuleCodeAsync(permissionRequest, expectedModuleCode);

        if (!string.Equals(expectedModuleCode, InstitutionDeviceConfigurationModuleCode, StringComparison.Ordinal))
        {
            throw new ForbiddenAccessException(AppConstants.ErrorMessages.PermissionDenied);
        }

        if (principal.UserType != LoginUserType.InstitutionStaffMember)
        {
            throw new ForbiddenAccessException(AppConstants.ErrorMessages.PermissionDenied);
        }

        var institutionContext = await commonRequestService.ValidateInstitutionUserRequestAsync();
        if (!institutionContext.Success ||
            institutionContext.InstitutionId <= 0 ||
            institutionContext.LoggedInStaffMemberId <= 0 ||
            institutionContext.RoleId <= 0)
        {
            throw new UnauthorizedAccessException(
                institutionContext.ErrorMessage ?? AppConstants.ErrorMessages.Unauthorized);
        }

        var permissionResult = await unitOfWork.StoreProcedureRepository.CheckInstitutionStaffMemberPermissionAsync(
            institutionContext.InstitutionId,
            institutionContext.LoggedInStaffMemberId,
            institutionContext.RoleId,
            permissionRequest.ModuleId,
            permissionRequest.OperationId,
            cancellationToken);
        InstitutionRuntimePermissionValidator.EnsureAllowed(permissionResult);

        return await next();
    }

    #endregion

    #region Module Ownership Mapping

    /// <summary>Resolves the leaf module that owns only the secured device configuration request set.</summary>
    private static string? ResolveExpectedModuleCode()
    {
        var requestType = typeof(TRequest);
        if (requestType == typeof(IssueInitialDeviceBootstrapCommand))
        {
            return HostInitialDeviceConfigurationModuleCode;
        }

        if (requestType.Namespace?.StartsWith("skolenza.application.Features.HostDeviceCmd.Handlers", StringComparison.Ordinal) == true && requestType.Name.Contains("InstitutionCardMaster", StringComparison.Ordinal))
            return HostInstitutionCardInventoryModuleCode;

        return requestType == typeof(CreateInstitutionDeviceConfigurationCommand) ||
               requestType == typeof(UpdateInstitutionDeviceConfigurationCommand) ||
               requestType == typeof(DeleteInstitutionDeviceConfigurationCommand) ||
               requestType == typeof(GetInstitutionDeviceConfigurationByIdQuery) ||
               requestType == typeof(GetAllInstitutionDeviceConfigurationsQuery) ||
               requestType == typeof(RotateInstitutionDeviceHttpsIngressTokenCommand) ||
               requestType == typeof(ApplyInstitutionDeviceRuntimeConfigurationCommand) ||
               requestType == typeof(RebootInstitutionDeviceCommand) ||
               requestType == typeof(ApplyInstitutionDeviceSettingsCommand) ||
               requestType == typeof(SyncInstitutionDeviceTimeCommand) ||
               requestType == typeof(UpdateInstitutionDeviceLocationCommand) ||
               requestType == typeof(GetInstitutionDeviceGatewayAddressQuery) ||
               requestType == typeof(ReplaceInstitutionDeviceHttpsGatewayUrlCommand) ||
               requestType == typeof(DispatchInstitutionDeviceMqttsNowCommand)
             ? InstitutionDeviceConfigurationModuleCode
             : null;
    }

    /// <summary>
    /// Host users may read configuration under their Host module grant, provision
    /// inventory, and issue a gateway URL. Institution admins own runtime mutations.
    /// </summary>
    private static bool IsHostAllowedRequest() =>
        IsConfigurationReadRequest() ||
        typeof(TRequest) == typeof(IssueInitialDeviceBootstrapCommand) ||
        typeof(TRequest) == typeof(RotateInstitutionDeviceHttpsIngressTokenCommand) ||
        typeof(TRequest).Name.Contains("InstitutionCardMaster", StringComparison.Ordinal);

    private static bool IsConfigurationReadRequest() =>
        typeof(TRequest) == typeof(GetAllInstitutionDeviceConfigurationsQuery) ||
        typeof(TRequest) == typeof(GetInstitutionDeviceConfigurationByIdQuery);

    #endregion

    #region Request Contract Resolution

    /// <summary>Checks that client-supplied module and operation IDs refer to the expected active leaf module.</summary>
    private async Task EnsureExpectedModuleCodeAsync(
        PermissionRequestDTO permissionRequest,
        string expectedModuleCode)
    {
        if (permissionRequest.ModuleId <= 0 || permissionRequest.OperationId <= 0)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        }

        var moduleCode = await commonRequestService.GetModuleCodeAsync(permissionRequest.ModuleId);
        if (string.Equals(moduleCode, expectedModuleCode, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        logger.LogWarning(
            "Device configuration module-code mismatch for {RequestName}. ModuleId: {ModuleId}; ModuleCode: {ModuleCode}; ExpectedModuleCode: {ExpectedModuleCode}",
            typeof(TRequest).Name,
            permissionRequest.ModuleId,
            moduleCode,
            expectedModuleCode);
        throw new ForbiddenAccessException(AppConstants.ErrorMessages.PermissionDenied);
    }

    /// <summary>Retrieves the request DTO carrying ModuleId and OperationId, following the existing StaffMember behavior convention.</summary>
    private static PermissionRequestDTO? ResolvePermissionRequest(TRequest request)
    {
        foreach (var memberName in new[] { "DTO", "Filter", "AccessRequest", "PermissionRequest" })
        {
            var property = typeof(TRequest).GetProperty(memberName, BindingFlags.Public | BindingFlags.Instance);
            if (property?.GetValue(request) is PermissionRequestDTO permissionRequest)
            {
                return permissionRequest;
            }

            var field = typeof(TRequest).GetField(memberName, BindingFlags.Public | BindingFlags.Instance);
            if (field?.GetValue(request) is PermissionRequestDTO fieldPermissionRequest)
            {
                return fieldPermissionRequest;
            }
        }

        return null;
    }

    #endregion
}
