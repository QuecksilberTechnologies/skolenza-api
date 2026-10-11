// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Binds Campus and staffMember-code requests to their active module before the
//           established Host or Institution runtime permission flow is used.
// ================================================================

using System.Reflection;
using skolenza.application.Common.Enums;
using skolenza.application.Common.Helpers;
using skolenza.application.Constants;
using skolenza.application.DTOs.BaseDTO;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.InstitutionConfigurationCmd;

/// <summary>
/// Centrally authorizes the six Campus requests and two staffMember-code writes. Host
/// roles use persisted module-operation permissions and Institution staffMembers use
/// the institution permission function without changing endpoint contracts.
/// </summary>
public sealed class InstitutionLocationPermissionBehavior<TRequest, TResponse>(
    IUnitOfWork unitOfWork,
    ICommonRequestService commonRequestService,
    ILogger<InstitutionLocationPermissionBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!IsCampusRequest() && !IsStaffMemberCodePatternRequest())
        {
            return await next();
        }

        var permissionRequest = ResolvePermissionRequest(request)
            ?? throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        var principal = await commonRequestService.ValidateAuthenticatedRequestAsync();

        if (principal.UserType == LoginUserType.Host)
        {
            if (IsStaffMemberCodePatternRequest())
            {
                throw new ForbiddenAccessException(AppConstants.ErrorMessages.PermissionDenied);
            }

            await HostRuntimePermissionValidator.ValidateAsync(
                commonRequestService,
                unitOfWork.StoreProcedureRepository,
                permissionRequest.ModuleId,
                permissionRequest.OperationId,
                cancellationToken);

            await EnsureExpectedModuleCodeAsync(permissionRequest, cancellationToken, LoginUserType.Host);
            return await next();
        }

        if (principal.UserType != LoginUserType.InstitutionStaffMember)
        {
            throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
        }

        await EnsureExpectedModuleCodeAsync(permissionRequest, cancellationToken);

        var validation = await commonRequestService.ValidateInstitutionUserRequestAsync();
        if (!validation.Success)
        {
            throw new UnauthorizedAccessException(
                validation.ErrorMessage ?? AppConstants.ErrorMessages.Unauthorized);
        }

        if (validation.InstitutionId <= 0 || validation.LoggedInStaffMemberId <= 0 || validation.RoleId <= 0)
        {
            throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
        }

        var permissionResult = await unitOfWork.StoreProcedureRepository
            .CheckInstitutionStaffMemberPermissionAsync(
                validation.InstitutionId,
                validation.LoggedInStaffMemberId,
                validation.RoleId,
                permissionRequest.ModuleId,
                permissionRequest.OperationId,
                cancellationToken);

        InstitutionRuntimePermissionValidator.EnsureAllowed(permissionResult);
        return await next();
    }

    /// <summary>
    /// Enforces the seeded leaf-module code after the request principal type is
    /// known. A mismatched client-supplied ModuleId is permission denied.
    /// </summary>
    private async Task EnsureExpectedModuleCodeAsync(
        PermissionRequestDTO permissionRequest,
        CancellationToken cancellationToken,
        LoginUserType userType = LoginUserType.InstitutionStaffMember)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var moduleCode = await commonRequestService
            .GetModuleCodeAsync(permissionRequest.ModuleId);
        var expectedModuleCode = IsStaffMemberCodePatternRequest()
            ? BulkImportConstants.StaffMemberCodeModuleCode
            : userType == LoginUserType.Host
                ? "HOST_CAMPUS_LIST"
                : "INSTITUTION_CAMPUSES";
        if (string.Equals(moduleCode, expectedModuleCode, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        logger.LogWarning(
            "Institution configuration module-code mismatch for {Request}. ModuleId: {ModuleId}, ModuleCode: {ModuleCode}, ExpectedModuleCode: {ExpectedModuleCode}",
            typeof(TRequest).Name,
            permissionRequest.ModuleId,
            moduleCode,
            expectedModuleCode);
        throw new ForbiddenAccessException(AppConstants.ErrorMessages.PermissionDenied);
    }

    /// <summary>
    /// Retrieves the existing permission DTO shape used by the location
    /// controller's commands and queries.
    /// </summary>
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

    /// <summary>
    /// Identifies only the Campus request types; other Institution
    /// configuration endpoints continue using their existing authorization.
    /// </summary>
    private static bool IsCampusRequest() => typeof(TRequest).Name is
        "CreateCampusCommand" or
        "UpdateCampusCommand" or
        "DeleteCampusCommand" or
        "UpdateCampusStatusCommand" or
        "GetCampusByIdQuery" or
        "GetCampussQuery";

    /// <summary>Reuses this configuration pipeline for the two institution pattern writes.</summary>
    private static bool IsStaffMemberCodePatternRequest()
    {
        return typeof(TRequest) == typeof(Configuration.StaffCodeCmd.Handlers.CreateStaffMemberCodePatternCommand) ||
               typeof(TRequest) == typeof(Configuration.StaffCodeCmd.Handlers.UpdateStaffMemberCodePatternCommand);
    }
}
