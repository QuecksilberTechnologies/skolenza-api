// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Enforces the trusted Institution role-permission contract for StaffMemberCmd requests.
// ================================================================

using skolenza.application.Common.Helpers;
using skolenza.application.Common.Enums;
using skolenza.application.Features.StaffMemberCmd.UpdateStatus.Handler;
using skolenza.application.Features.StaffMemberCmd.UpdateVerification.Handler;
using skolenza.application.Constants;
using skolenza.application.DTOs.BaseDTO;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace skolenza.application.Features.StaffMemberCmd;

/// <summary>
/// Applies the existing Institution request and database-backed role permission
/// validation consistently to every StaffMemberCmd command and query.
/// </summary>
/// <typeparam name="TRequest">The MediatR command or query.</typeparam>
/// <typeparam name="TResponse">The command or query response.</typeparam>
public sealed class StaffMemberInstitutionPermissionBehavior<TRequest, TResponse>(
    IUnitOfWork unitOfWork,
    ICommonRequestService commonRequestService,
    IIdEncoderService idEncoderService,
    ILogger<StaffMemberInstitutionPermissionBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // This behavior is deliberately limited to StaffMemberCmd. Host pages do
        // not use these endpoints and therefore cannot receive a Host bypass.
        var requestNamespace = typeof(TRequest).Namespace;
        if (requestNamespace?.StartsWith(
                "skolenza.application.Features.StaffMemberCmd",
                StringComparison.Ordinal) != true)
        {
            return await next();
        }

        // Like Role/type-options, this enum-only endpoint is protected by [Authorize]
        // at the HTTP boundary. The user explicitly requested token verification only:
        // no institution/user persistence, identifier decoding or module permission lookup.
        if (request is Contact.Handlers.GetContactRelationOptionsQuery or
            StaffCategory.Handlers.GetStaffCategoryOptionsQuery)
        {
            return await next();
        }

        var validation = await commonRequestService.ValidateInstitutionUserRequestAsync();
        if (!validation.Success)
        {
            throw new UnauthorizedAccessException(
                validation.ErrorMessage ?? AppConstants.ErrorMessages.Unauthorized);
        }

        // Attendance options retain their existing validated Institution context and no module operation.
        if (typeof(TRequest) == typeof(StaffMemberWorkInfo.Handlers.GetAttendancePolicyOptionsQuery))
        {
            return await next();
        }

        var permissionRequest = ResolvePermissionRequest(request);
        if (permissionRequest is null)
        {
            logger.LogWarning(
                "StaffMember request {StaffMemberRequest} did not provide ModuleId and OperationId.",
                typeof(TRequest).Name);
            throw new ForbiddenAccessException(AppConstants.ErrorMessages.PermissionDenied);
        }

        var expectedModuleCode = ResolveExpectedModuleCode();
        int? statusTab = request switch
        {
            UpdateEditableStatusCommand edit => edit.DTO.TabInfoType,
            UpdateVerificationStatusCommand verify => verify.DTO.TabInfoType,
            _ => null
        };
        if (statusTab.HasValue)
        {
            expectedModuleCode = (TabInfoType)statusTab.Value switch
            {
                TabInfoType.StaffMember => "STAFF_OVERVIEW",
                TabInfoType.Bank => "STAFF_BANK_DETAILS",
                TabInfoType.Contact => "STAFF_CONTACT_DETAILS",
                TabInfoType.Experience => "STAFF_EXPERIENCE",
                TabInfoType.Identity => "STAFF_IDENTITY",
                TabInfoType.Education => "STAFF_QUALIFICATIONS",
                TabInfoType.Dependent => "STAFF_DEPENDANTS",
                _ => null
            };
        }
        if (string.IsNullOrWhiteSpace(expectedModuleCode))
        {
            logger.LogWarning(
                "No expected module-code mapping exists for StaffMember request {StaffMemberRequest}.",
                typeof(TRequest).FullName);
            throw new ForbiddenAccessException(AppConstants.ErrorMessages.PermissionDenied);
        }

        var moduleCode = await commonRequestService
            .GetModuleCodeAsync(permissionRequest.ModuleId);
        if (!string.Equals(moduleCode, expectedModuleCode, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning(
                "StaffMember module-code mismatch for {StaffMemberRequest}. ModuleId: {ModuleId}, ModuleCode: {ModuleCode}, ExpectedModuleCode: {ExpectedModuleCode}",
                typeof(TRequest).Name,
                permissionRequest.ModuleId,
                moduleCode,
                expectedModuleCode);
            throw new ForbiddenAccessException(AppConstants.ErrorMessages.PermissionDenied);
        }

        long institutionId = validation.InstitutionId;
        long userStaffMemberId = validation.LoggedInStaffMemberId;
        int tokenRoleId = validation.RoleId;
        if (institutionId <= 0 || userStaffMemberId <= 0 || tokenRoleId <= 0)
        {
            logger.LogWarning(
                "Invalid Institution authorization context for {StaffMemberRequest}. InstitutionId: {InstitutionId}, StaffMemberId: {StaffMemberId}, TokenRoleId: {TokenRoleId}",
                typeof(TRequest).Name,
                institutionId,
                userStaffMemberId,
                tokenRoleId);
            throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
        }

        // The stored procedure uses the current role assignments; the JWT role
        // is only a context value and is never the final authorization source.
        var permissionResult = await unitOfWork.StoreProcedureRepository
            .CheckInstitutionStaffMemberPermissionAsync(
                institutionId,
                userStaffMemberId,
                tokenRoleId,
                permissionRequest.ModuleId,
                permissionRequest.OperationId,
                cancellationToken);

        InstitutionRuntimePermissionValidator.EnsureAllowed(permissionResult);

        // Operational defaults constrain staffMember self-service, including record-id commands.
        // Existing role/action and target-ownership checks remain mandatory.
        if (validation.RoleTypeId == ConstantValues.RoleTypeStaffMember &&
            StaffMemberOperationalSections.ModuleCodes.Contains(expectedModuleCode) &&
            typeof(TRequest).Name.EndsWith("Command", StringComparison.Ordinal) &&
            !await unitOfWork.StaffMembers.IsOperationalSectionEditAllowedAsync(
                institutionId, expectedModuleCode, cancellationToken))
        {
            throw new ForbiddenAccessException(AppConstants.ErrorMessages.PermissionDenied);
        }

        await EnsureStaffMemberTargetAccessAsync(
            request,
            validation,
            expectedModuleCode,
            idEncoderService,
            commonRequestService,
            cancellationToken);
        return await next();
    }

    /// <summary>
    /// Applies staffMember-data scope before a handler can load a client-selected staffMember. This
    /// complements the stored-procedure action grant: a role may invoke an endpoint, but still
    /// may not use that endpoint to read or change another staffMember's private data.
    /// </summary>
    private static async Task EnsureStaffMemberTargetAccessAsync(
        TRequest request,
        Common.Models.Security.CommonDecodedResult validation,
        string expectedModuleCode,
        IIdEncoderService idEncoderService,
        ICommonRequestService commonRequestService,
        CancellationToken cancellationToken)
    {
        // The paged directory handles heterogeneous visibility in its repository and projection:
        // Admin sees full rows, StaffMember sees own full row plus colleagues' directory fields, and
        // Manager receives self plus direct reports. It must not be reduced to one target here.
        if (typeof(TRequest).Name == "GetAllStaffMemberInfoQuery")
        {
            return;
        }

        var encodedStaffMemberId = ResolveStaffMemberId(request);
        if (string.IsNullOrWhiteSpace(encodedStaffMemberId))
        {
            // Record-id commands resolve the owner in their handler/repository. No client value is
            // inferred as an staffMember target here.
            return;
        }

        long targetStaffMemberId;
        try
        {
            targetStaffMemberId = Common.Helpers.RequestHelper.RequestCommonHelper.DecodeOnlyStaffMemberId(
                encodedStaffMemberId,
                validation.Claims.InstitutionEncriptionKey,
                idEncoderService);
        }
        catch (Exception)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
        }

        if (targetStaffMemberId <= 0)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
        }

        var requirement = expectedModuleCode == "STAFF_CAMPUS_ASSIGNMENTS"
            ? StaffMemberDataAccessRequirement.WorkLocation
            : typeof(TRequest).Name is "GetStaffMemberProfileSummaryQuery" or "GetStaffMemberImageQuery"
                ? StaffMemberDataAccessRequirement.DirectoryBasic
                : StaffMemberDataAccessRequirement.PersonalDetails;

        if (!await commonRequestService.CanAccessStaffMemberDataAsync(
                validation,
                targetStaffMemberId,
                requirement,
                cancellationToken))
        {
            throw new ForbiddenAccessException(AppConstants.ErrorMessages.PermissionDenied);
        }
    }

    /// <summary>
    /// Retrieves the DTO carrying ModuleId and OperationId without changing
    /// existing endpoint routes or response contracts.
    /// </summary>
    private static PermissionRequestDTO? ResolvePermissionRequest(TRequest request)
    {
        foreach (var propertyName in new[] { "DTO", "Filter", "PermissionRequest" })
        {
            var property = typeof(TRequest).GetProperty(
                propertyName,
                BindingFlags.Public | BindingFlags.Instance);

            if (property?.GetValue(request) is PermissionRequestDTO permissionRequest)
            {
                return permissionRequest;
            }

            var field = typeof(TRequest).GetField(
                propertyName,
                BindingFlags.Public | BindingFlags.Instance);
            if (field?.GetValue(request) is PermissionRequestDTO fieldPermissionRequest)
            {
                return fieldPermissionRequest;
            }
        }

        return null;
    }

    /// <summary>
    /// Obtains a direct StaffMemberId only from the known request/DTO shapes. It intentionally does
    /// not inspect arbitrary string fields, avoiding accidental authorization decisions based on
    /// unrelated client data.
    /// </summary>
    private static string? ResolveStaffMemberId(TRequest request)
    {
        foreach (var owner in new object?[]
                 {
                     request,
                     typeof(TRequest).GetProperty("DTO", BindingFlags.Public | BindingFlags.Instance)?.GetValue(request),
                     typeof(TRequest).GetProperty("Filter", BindingFlags.Public | BindingFlags.Instance)?.GetValue(request)
                 })
        {
            if (owner is null)
            {
                continue;
            }

            var staffMemberIdProperty = owner.GetType().GetProperty(
                "StaffMemberId",
                BindingFlags.Public | BindingFlags.Instance);
            if (staffMemberIdProperty?.GetValue(owner) is string staffMemberId)
            {
                return staffMemberId;
            }
        }

        return null;
    }

    /// <summary>
    /// Resolves the StaffMember leaf-module code that owns the request. Header modules are never
    /// used for authorization; every request binds to the UI leaf module from the approved list.
    /// </summary>
    private static string? ResolveExpectedModuleCode()
    {
        var requestNamespace = typeof(TRequest).Namespace ?? string.Empty;

        if (requestNamespace.StartsWith("skolenza.application.Features.StaffMemberCmd.BankInfo", StringComparison.Ordinal))
            return "STAFF_BANK_DETAILS";
        if (requestNamespace.StartsWith("skolenza.application.Features.StaffMemberCmd.Contact", StringComparison.Ordinal))
            return "STAFF_CONTACT_DETAILS";
        if (requestNamespace.StartsWith("skolenza.application.Features.StaffMemberCmd.DependentInfo", StringComparison.Ordinal))
            return "STAFF_DEPENDANTS";
        if (requestNamespace.StartsWith("skolenza.application.Features.StaffMemberCmd.EducationInfo", StringComparison.Ordinal))
            return "STAFF_QUALIFICATIONS";
        if (requestNamespace.StartsWith("skolenza.application.Features.StaffMemberCmd.ExperienceInfo", StringComparison.Ordinal))
            return "STAFF_EXPERIENCE";
        if (requestNamespace.StartsWith("skolenza.application.Features.StaffMemberCmd.IdentitiesInfo", StringComparison.Ordinal))
            return "STAFF_IDENTITY";
        if (requestNamespace.StartsWith("skolenza.application.Features.StaffMemberCmd.StaffMemberDeviceEnrollment", StringComparison.Ordinal))
            return "STAFF_DEVICE_ENROLMENTS";
        if (requestNamespace.StartsWith("skolenza.application.Features.StaffMemberCmd.StaffMemberMonitoring", StringComparison.Ordinal))
            return "STAFF_DEVICE_ENROLMENTS";
        if (requestNamespace.StartsWith("skolenza.application.Features.StaffMemberCmd.ResetPassword", StringComparison.Ordinal))
            return "STAFF_DIRECTORY";
        if (requestNamespace.StartsWith("skolenza.application.Features.StaffMemberCmd.StaffMemberWorkInfo", StringComparison.Ordinal))
        {
            var requestName = typeof(TRequest).Name;
            if (requestName.Contains("StaffMemberLocationAssignment", StringComparison.Ordinal))
                return "STAFF_CAMPUS_ASSIGNMENTS";
            if (requestName.Contains("StaffMemberWorkArrangement", StringComparison.Ordinal))
                return "STAFF_WORK_ARRANGEMENTS";
            if (requestName.Contains("StaffMemberWorkPattern", StringComparison.Ordinal))
                return "STAFF_WORK_PATTERNS";
            if (requestName.Contains("StaffMemberWorkModeOverride", StringComparison.Ordinal))
                return "STAFF_WORK_MODE_OVERRIDES";

            return null;
        }
        if (requestNamespace.StartsWith("skolenza.application.Features.StaffMemberCmd.UpdateStatus", StringComparison.Ordinal) ||
            requestNamespace.StartsWith("skolenza.application.Features.StaffMemberCmd.UpdateVerification", StringComparison.Ordinal))
            return "STAFF_DIRECTORY";
        if (requestNamespace.StartsWith("skolenza.application.Features.StaffMemberCmd.ProfessionalStructure", StringComparison.Ordinal))
            return "STAFF_DIRECTORY";

        if (!requestNamespace.StartsWith("skolenza.application.Features.StaffMemberCmd.StaffMemberBase", StringComparison.Ordinal))
        {
            return null;
        }

        return typeof(TRequest).Name switch
        {
            "CreateBaseStaffMemberInfoCommand" or
            "PreviewStaffMemberImportQuery" or
            "ManageStaffMemberImportCommand" or
            "GetAllStaffMemberInfoQuery" or
            "GetStaffMemberSummaryQuery" or
            "DeleteStaffMemberQuery" or
            "ActivateAllStaffMemberQuery" or
            "UpdateSectionBulkCommand" => "STAFF_DIRECTORY",

            "UpdateStaffMemberSectionDefaultsCommand" => "STAFF_DIRECTORY",

            "GetBaseStaffMemberInfoQuery" or
            "UpdateStaffMemberCommand" or
            "UpdateBaseStaffMemberByAdminCommand" or
            "GetStaffMemberImageQuery" or
            "CreateStaffMemberImageCommand" or
            "UpdateProfileImageCommand" or
            "GetStaffMemberProfileSummaryQuery" or
            "GetStaffMemberProfileStatusQuery" => "STAFF_OVERVIEW",

            _ => null
        };
    }
}
