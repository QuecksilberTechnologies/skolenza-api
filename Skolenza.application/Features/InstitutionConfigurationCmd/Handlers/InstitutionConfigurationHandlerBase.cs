// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Provides common authenticated Institution context and response helpers for InstitutionConfiguration handlers.
// ================================================================

using skolenza.application.Common.Helpers;
using skolenza.application.Common.Helpers.RequestHelper;
using skolenza.application.Common.Models.Security;
using skolenza.application.Constants;
using skolenza.application.DTOs.BaseDTO;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Wrappers;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.InstitutionConfigurationCmd.Handlers;

/// <summary>Provides common Institution authentication and paging-response behavior for this module.</summary>
public abstract class InstitutionConfigurationHandlerBase
{
    private readonly IIdEncoderService? _idEncoderService;

    /// <summary>Initializes common handler dependencies.</summary>
    protected InstitutionConfigurationHandlerBase(
        IUnitOfWork unitOfWork,
        ICommonRequestService commonRequestService,
        ILogger<InstitutionConfigurationHandlerBase> logger,
        IIdEncoderService? idEncoderService = null)
    {
        UnitOfWork = unitOfWork;
        CommonRequestService = commonRequestService;
        Logger = logger;
        _idEncoderService = idEncoderService;
    }

    /// <summary>Gets the unit of work used by InstitutionConfiguration handlers.</summary>
    protected IUnitOfWork UnitOfWork { get; }

    /// <summary>Gets the service that validates the authenticated Institution principal.</summary>
    protected ICommonRequestService CommonRequestService { get; }

    /// <summary>Gets the structured logger for InstitutionConfiguration activity.</summary>
    protected ILogger<InstitutionConfigurationHandlerBase> Logger { get; }

    /// <summary>Resolves the trusted Institution and staffMember audit actor from the current request.</summary>
    /// <returns>The authenticated Institution identifier and staffMember identifier.</returns>
    protected async Task<(long InstitutionId, long ActorId)> ValidateInstitutionAsync()
    {
        var validation = await CommonRequestService.ValidateInstitutionUserRequestAsync();
        if (!validation.Success)
        {
            throw new UnauthorizedAccessException(validation.ErrorMessage);
        }

        return (validation.InstitutionId, validation.LoggedInStaffMemberId);
    }

    /// <summary>
    /// Resolves the trusted institution request context for handlers that first need to load a
    /// record and then verify access against that record's owning staffMember.
    /// </summary>
    protected async Task<CommonDecodedResult> ValidateInstitutionDataAccessContextAsync()
    {
        var validation = await CommonRequestService.ValidateInstitutionUserRequestAsync();
        if (!validation.Success || validation.InstitutionId <= 0 || validation.LoggedInStaffMemberId <= 0)
        {
            throw new UnauthorizedAccessException(
                validation.ErrorMessage ?? AppConstants.ErrorMessages.Unauthorized);
        }

        return validation;
    }

    /// <summary>
    /// Enforces the central staffMember ownership rule after a target staffMember has been decoded or
    /// resolved from a institution-scoped record. A denied target always returns the standard 403 path.
    /// </summary>
    protected async Task EnsureStaffMemberDataAccessAsync(
        CommonDecodedResult validation,
        long targetStaffMemberId,
        StaffMemberDataAccessRequirement requirement,
        CancellationToken cancellationToken)
    {
        var hasAccess = await CommonRequestService.CanAccessStaffMemberDataAsync(
            validation,
            targetStaffMemberId,
            requirement,
            cancellationToken);
        if (!hasAccess)
        {
            throw new ForbiddenAccessException(AppConstants.ErrorMessages.PermissionDenied);
        }
    }

    /// <summary>
    /// Validates the institution principal and decodes a required client-facing staffMember identifier.
    /// The encoded identifier is never passed to entities or repositories.
    /// </summary>
    protected async Task<(long InstitutionId, long ActorId, long StaffMemberId)> ValidateInstitutionAndDecodeStaffMemberIdAsync(
        string? encodedStaffMemberId,
        StaffMemberDataAccessRequirement requirement = StaffMemberDataAccessRequirement.PersonalDetails,
        CancellationToken cancellationToken = default)
    {
        var context = await ValidateInstitutionAndDecodeOptionalStaffMemberIdAsync(
            encodedStaffMemberId,
            requirement,
            cancellationToken);
        if (!context.StaffMemberId.HasValue)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
        }

        return (context.InstitutionId, context.ActorId, context.StaffMemberId.Value);
    }

    /// <summary>Encodes an staffMember identifier with the authenticated institution salt for API responses.</summary>
    protected string EncodeStaffMemberId(long staffMemberId, CommonDecodedResult validation)
    {
        if (_idEncoderService is null || string.IsNullOrWhiteSpace(validation.Claims.InstitutionEncriptionKey))
        {
            throw new InvalidOperationException("The staffMember identifier encoder is not configured.");
        }

        return _idEncoderService.EncodeId_long(
            staffMemberId,
            validation.Claims.InstitutionEncriptionKey);
    }

    /// <summary>
    /// Validates the institution principal and decodes an optional staffMember filter when the client supplied one.
    /// </summary>
    protected async Task<(long InstitutionId, long ActorId, long? StaffMemberId)> ValidateInstitutionAndDecodeOptionalStaffMemberIdAsync(
        string? encodedStaffMemberId,
        StaffMemberDataAccessRequirement requirement = StaffMemberDataAccessRequirement.PersonalDetails,
        CancellationToken cancellationToken = default)
    {
        var validation = await CommonRequestService.ValidateInstitutionUserRequestAsync();
        if (!validation.Success ||
            validation.InstitutionId <= 0 ||
            validation.LoggedInStaffMemberId <= 0 ||
            string.IsNullOrWhiteSpace(validation.Claims.InstitutionEncriptionKey))
        {
            throw new UnauthorizedAccessException(
                validation.ErrorMessage ?? AppConstants.ErrorMessages.Unauthorized);
        }

        if (string.IsNullOrWhiteSpace(encodedStaffMemberId))
        {
            return (validation.InstitutionId, validation.LoggedInStaffMemberId, null);
        }

        if (_idEncoderService is null)
        {
            throw new InvalidOperationException("The staffMember identifier decoder is not configured.");
        }

        try
        {
            var staffMemberId = RequestCommonHelper.DecodeOnlyStaffMemberId(
                encodedStaffMemberId,
                validation.Claims.InstitutionEncriptionKey,
                _idEncoderService);

            if (staffMemberId <= 0)
            {
                throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
            }

            await EnsureStaffMemberDataAccessAsync(
                validation,
                staffMemberId,
                requirement,
                cancellationToken);

            return (validation.InstitutionId, validation.LoggedInStaffMemberId, staffMemberId);
        }
        catch (ValidationErrorException)
        {
            throw;
        }
        catch (Exception)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
        }
    }

    /// <summary>
    /// Resolves the authenticated Institution context and enforces the requested
    /// module operation using the current database role assignments.
    /// </summary>
    /// <param name="request">The request carrying the client-supplied module and operation identifiers.</param>
    /// <param name="cancellationToken">Token used to cancel the authorization operation.</param>
    /// <returns>The trusted Institution identifier and staffMember audit actor.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when the Institution context is invalid, stale, or denied.</exception>
    protected async Task<(long InstitutionId, long ActorId)> ValidateInstitutionPermissionAsync(
        PermissionRequestDTO request,
        CancellationToken cancellationToken)
    {
        var validation = await CommonRequestService.ValidateInstitutionUserRequestAsync();
        if (!validation.Success)
        {
            throw new UnauthorizedAccessException(
                validation.ErrorMessage ?? AppConstants.ErrorMessages.Unauthorized);
        }

        long institutionId = validation.InstitutionId;
        long actorId = validation.LoggedInStaffMemberId;
        int tokenRoleId = validation.RoleId;
        if (institutionId <= 0 || actorId <= 0 || tokenRoleId <= 0)
        {
            Logger.LogWarning(
                "Invalid Institution authorization context. InstitutionId: {InstitutionId}, StaffMemberId: {StaffMemberId}, TokenRoleId: {TokenRoleId}",
                institutionId,
                actorId,
                tokenRoleId);
            throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
        }

        var permissionResult = await UnitOfWork.StoreProcedureRepository
            .CheckInstitutionStaffMemberPermissionAsync(
                institutionId,
                actorId,
                tokenRoleId,
                request.ModuleId,
                request.OperationId,
                cancellationToken);

        InstitutionRuntimePermissionValidator.EnsureAllowed(permissionResult);
        return (institutionId, actorId);
    }

    /// <summary>Creates a flattened paginated API response without data nesting.</summary>
    protected static ApiResponse<List<TResponse>> Paged<TResponse>(List<TResponse> data, int pageNumber, int pageSize, int totalRecords, string message) =>
        ApiResponse<List<TResponse>>.SuccessPaginated(data, pageNumber, pageSize, totalRecords, (int)Math.Ceiling(totalRecords / (double)pageSize), message);
}
