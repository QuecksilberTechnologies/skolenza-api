// ================================================================
// Purpose : Enforces Host and Institution runtime permission checks for Institution SMTP configuration management.
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

namespace skolenza.application.Features.InstitutionEmailConfigCmd;

/// <summary>
/// Validates the separate Host and Institution module operations for Institution SMTP
/// configuration. Every Host role is checked against current database
/// permissions; Institution staffMembers use the institution permission function.
/// </summary>
public sealed class InstitutionEmailConfigPermissionBehavior<TRequest, TResponse>(
    IUnitOfWork unitOfWork,
    ICommonRequestService commonRequestService,
    ILogger<InstitutionEmailConfigPermissionBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const string HostInstitutionEmailConfigModuleCode = "HOST_INSTITUTION_EMAIL_CONFIG";
    private const string InstitutionEmailConfigModuleCode = "INSTITUTION_EMAIL_CONFIG";

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (typeof(TRequest).Namespace?.StartsWith(
                "skolenza.application.Features.InstitutionEmailConfigCmd",
                StringComparison.Ordinal) != true)
        {
            return await next();
        }

        var permissionRequest = ResolvePermissionRequest(request)
            ?? throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        var principal = await commonRequestService.ValidateAuthenticatedRequestAsync();

        if (principal.UserType == LoginUserType.Host)
        {
            await HostRuntimePermissionValidator.ValidateAsync(
                commonRequestService,
                unitOfWork.StoreProcedureRepository,
                permissionRequest.ModuleId,
                permissionRequest.OperationId,
                cancellationToken);

            await EnsureExpectedModuleCodeAsync(permissionRequest, HostInstitutionEmailConfigModuleCode);

            return await next();
        }

        if (principal.UserType != LoginUserType.InstitutionStaffMember)
        {
            throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
        }

        await EnsureExpectedModuleCodeAsync(permissionRequest, InstitutionEmailConfigModuleCode);
        var institutionContext = await commonRequestService.ValidateInstitutionUserRequestAsync();
        if (!institutionContext.Success ||
            institutionContext.InstitutionId <= 0 ||
            institutionContext.LoggedInStaffMemberId <= 0 ||
            institutionContext.RoleId <= 0)
        {
            throw new UnauthorizedAccessException(
                institutionContext.ErrorMessage ?? AppConstants.ErrorMessages.Unauthorized);
        }

        var permissionResult = await unitOfWork.StoreProcedureRepository
            .CheckInstitutionStaffMemberPermissionAsync(
                institutionContext.InstitutionId,
                institutionContext.LoggedInStaffMemberId,
                institutionContext.RoleId,
                permissionRequest.ModuleId,
                permissionRequest.OperationId,
                cancellationToken);
        InstitutionRuntimePermissionValidator.EnsureAllowed(permissionResult);

        return await next();
    }

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
            "Institution email configuration module-code mismatch for {RequestName}. ModuleId: {ModuleId}; ModuleCode: {ModuleCode}; ExpectedModuleCode: {ExpectedModuleCode}",
            typeof(TRequest).Name,
            permissionRequest.ModuleId,
            moduleCode,
            expectedModuleCode);
        throw new ForbiddenAccessException(AppConstants.ErrorMessages.PermissionDenied);
    }

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
}
