// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Binds Host Institution-management requests to their active ModuleCode
//           before the established Host runtime permission flow is used.
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

namespace skolenza.application.Features.InstitutionManagementCmd;

/// <summary>
/// Enforces the Host Institution-management module contract for every command and
/// query in <c>InstitutionManagementCmd</c>. Every Host role is checked through the
/// persisted <see cref="HostRuntimePermissionValidator"/> flow.
/// </summary>
public sealed class InstitutionManagementPermissionBehavior<TRequest, TResponse>(
    IUnitOfWork unitOfWork,
    ICommonRequestService commonRequestService,
    ILogger<InstitutionManagementPermissionBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (typeof(TRequest).Namespace?.StartsWith(
                "skolenza.application.Features.InstitutionManagementCmd",
                StringComparison.Ordinal) != true)
        {
            return await next();
        }

        var expectedModuleCodes = ResolveExpectedModuleCodes();
        if (expectedModuleCodes is null || expectedModuleCodes.Length == 0)
        {
            logger.LogWarning(
                "No Host Institution-management module-code binding exists for request {InstitutionRequest}.",
                typeof(TRequest).FullName);
            throw new ForbiddenAccessException(AppConstants.ErrorMessages.PermissionDenied);
        }

        var permissionRequest = ResolvePermissionRequest(request);
        await HostRuntimePermissionValidator.ValidateAsync(
            commonRequestService,
            unitOfWork.StoreProcedureRepository,
            permissionRequest?.ModuleId ?? 0,
            permissionRequest?.OperationId ?? 0,
            cancellationToken);

        if (permissionRequest is null)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        }

        var activeModuleCode = await commonRequestService
            .GetActiveModuleCodeAsync(permissionRequest.ModuleId);
        if (string.IsNullOrWhiteSpace(activeModuleCode) ||
            !Array.Exists(
                expectedModuleCodes,
                expectedCode => string.Equals(activeModuleCode, expectedCode, StringComparison.OrdinalIgnoreCase)))
        {
            logger.LogWarning(
                "Host Institution-management module-code mismatch for {InstitutionRequest}. ModuleId: {ModuleId}, ActiveModuleCode: {ActiveModuleCode}, ExpectedModuleCodes: {ExpectedModuleCodes}",
                typeof(TRequest).Name,
                permissionRequest.ModuleId,
                activeModuleCode,
                string.Join(", ", expectedModuleCodes));
            throw new ForbiddenAccessException(AppConstants.ErrorMessages.PermissionDenied);
        }

        return await next();
    }

    /// <summary>
    /// Reads the existing request-carried module-operation values without
    /// changing controller signatures or public request contracts.
    /// </summary>
    private static PermissionRequestDTO? ResolvePermissionRequest(TRequest request)
    {
        var requestType = typeof(TRequest);
        var directModule = requestType.GetProperty("ModuleId", BindingFlags.Public | BindingFlags.Instance);
        var directOperation = requestType.GetProperty("OperationId", BindingFlags.Public | BindingFlags.Instance);
        if (directModule?.PropertyType == typeof(int) && directOperation?.PropertyType == typeof(int))
        {
            return new PermissionRequestDTO
            {
                ModuleId = (int)(directModule.GetValue(request) ?? 0),
                OperationId = (int)(directOperation.GetValue(request) ?? 0)
            };
        }

        foreach (var memberName in new[] { "RequestDTO", "DTO", "Filter", "PermissionRequest", "AccessRequest" })
        {
            var property = requestType.GetProperty(memberName, BindingFlags.Public | BindingFlags.Instance);
            if (property?.GetValue(request) is PermissionRequestDTO permissionRequest)
            {
                return permissionRequest;
            }

            var field = requestType.GetField(memberName, BindingFlags.Public | BindingFlags.Instance);
            if (field?.GetValue(request) is PermissionRequestDTO fieldPermissionRequest)
            {
                return fieldPermissionRequest;
            }
        }

        return null;
    }

    /// <summary>
    /// Resolves the Host Institution-management leaf module code(s) allowed for each
    /// request. The seeded header module is never an authorization target.
    /// </summary>
    private static string[]? ResolveExpectedModuleCodes() => typeof(TRequest).Name switch
    {
        // Create may be initiated from the Institution List row-action menu or from
        // the dedicated provisioning module. Both mappings remain permission-checked.
        "CreateNewInstitutionCommand" => ["HOST_INSTITUTION_LIST", "HOST_INSTITUTION_CREATE"],

        "GetAllInstitutionsQuery" or
        "GetInstitutionByIdQuery" or
        "GetInstitutionDeleteDependencyInfoQuery" or
        "UpdateInstitutionCommand" or
        "UpdateHostManagedInstitutionCommand" or
        "UpdateNewInstitutionCommand" or
        "SynchronizeInstitutionPlanEntitlementsCommand" or
        "ResendInstitutionVerificationCommand" or
        "DeleteHostManagedInstitutionCommand" or
        "ActivateInstitutionCommand" or
        "DeactivateInstitutionCommand" => ["HOST_INSTITUTION_LIST"],

        _ => null
    };
}
