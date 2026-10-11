// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Centrally enforces Designation module-operation permissions.
// ================================================================

using System.Reflection;
using skolenza.application.Common.Helpers;
using skolenza.application.Constants;
using skolenza.application.DTOs.BaseDTO;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.DesignationCmd;

/// <summary>
/// Enforces the current institution user's database-backed Designation permissions
/// before any Designation command or query reaches its handler.
/// </summary>
/// <typeparam name="TRequest">The MediatR command or query.</typeparam>
/// <typeparam name="TResponse">The command or query response.</typeparam>
public sealed class DesignationPermissionBehavior<TRequest, TResponse>(
    IUnitOfWork unitOfWork,
    ICommonRequestService commonRequestService,
    ILogger<DesignationPermissionBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const string DesignationModuleCode = "STAFF_DESIGNATIONS";

    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (typeof(TRequest).Namespace?.StartsWith(
                "skolenza.application.Features.DesignationCmd",
                StringComparison.Ordinal) != true)
        {
            return await next();
        }

        var permissionRequest = ResolvePermissionRequest(request)
            ?? throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);

        if (permissionRequest.ModuleId <= 0 || permissionRequest.OperationId <= 0)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        }

        var moduleCode = await commonRequestService
            .GetModuleCodeAsync(permissionRequest.ModuleId);
        if (!string.Equals(moduleCode, DesignationModuleCode, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning(
                "Designation module-code mismatch for {DesignationRequest}. ModuleId: {ModuleId}, ModuleCode: {ModuleCode}, ExpectedModuleCode: {ExpectedModuleCode}",
                typeof(TRequest).Name,
                permissionRequest.ModuleId,
                moduleCode,
                DesignationModuleCode);
            throw new ForbiddenAccessException(AppConstants.ErrorMessages.PermissionDenied);
        }

        var validation = await commonRequestService.ValidateInstitutionUserRequestAsync();
        if (!validation.Success)
        {
            throw new UnauthorizedAccessException(
                validation.ErrorMessage ?? AppConstants.ErrorMessages.Unauthorized);
        }

        if (validation.InstitutionId <= 0 ||
            validation.LoggedInStaffMemberId <= 0 ||
            validation.RoleId <= 0)
        {
            logger.LogWarning(
                "Invalid Institution authorization context for Designation request {DesignationRequest}. InstitutionId: {InstitutionId}, StaffMemberId: {StaffMemberId}, TokenRoleId: {TokenRoleId}",
                typeof(TRequest).Name,
                validation.InstitutionId,
                validation.LoggedInStaffMemberId,
                validation.RoleId);
            throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
        }

        // The stored function resolves the current primary and secondary role
        // assignments, so a valid but stale JWT cannot grant Designation access.
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
    /// Reads the existing command/query DTO shape without altering endpoint routes.
    /// </summary>
    private static PermissionRequestDTO? ResolvePermissionRequest(TRequest request)
    {
        foreach (var memberName in new[] { "DTO", "Dto", "OptionDTO", "Filter", "PermissionRequest" })
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
