// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Soft-deletes institution-owned asset types from authenticated requests.
// ================================================================

using skolenza.application.Common.Helpers;
using skolenza.application.Constants;
using skolenza.application.DTOS.AssetDTO.type;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.AssetFeatures.Type.Handlers;

#region Command

/// <summary>
/// Represents the request to delete an asset type.
/// </summary>
public class DeletetTypeCommand : IRequest<ApiResponse<bool>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeletetTypeCommand"/> class.
    /// </summary>
    /// <param name="dto">The asset type selected for deletion.</param>
    public DeletetTypeCommand(DeleteTypeRequestDTO dto)
    {
        DTO = dto;
    }

    /// <summary>
    /// Gets the asset type selected for deletion.
    /// </summary>
    public DeleteTypeRequestDTO DTO { get; }
}

#endregion

#region Handler

/// <summary>
/// Handles soft deletion of institution-owned asset types.
/// </summary>
public class DeletetTypeCommandHandler : IRequestHandler<DeletetTypeCommand, ApiResponse<bool>>
{
    #region Fields

    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeletetTypeCommandHandler> _logger;
    private readonly ICommonRequestService _commonRequestService;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="DeletetTypeCommandHandler"/> class.
    /// </summary>
    public DeletetTypeCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<DeletetTypeCommandHandler> logger,
        ICommonRequestService commonRequestService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _commonRequestService = commonRequestService;
    }

    #endregion

    #region Handle

    /// <inheritdoc />
    public async Task<ApiResponse<bool>> Handle(
        DeletetTypeCommand request,
        CancellationToken cancellationToken)
    {
        if (request.DTO is null || request.DTO.Id <= 0)
        {
            throw new ValidationErrorException(
                "Invalid Type Id.",
                new List<string> { "Type Id must be greater than 0." });
        }

        #region Institution Request Validation

        var validation = await _commonRequestService.ValidateInstitutionUserRequestAsync();
        if (!validation.Success)
        {
            throw new UnauthorizedAccessException(
                validation.ErrorMessage ?? AppConstants.ErrorMessages.Unauthorized);
        }

        #endregion

        #region Trusted Request Context

        long userStaffMemberId = validation.LoggedInStaffMemberId;
        long institutionId = validation.InstitutionId;
        int tokenRoleId = validation.RoleId;

        if (userStaffMemberId <= 0 || institutionId <= 0 || tokenRoleId <= 0)
        {
            _logger.LogWarning(
                "Invalid Institution authorization context while deleting Asset Type. InstitutionId: {InstitutionId}, StaffMemberId: {StaffMemberId}, TokenRoleId: {TokenRoleId}",
                institutionId, userStaffMemberId, tokenRoleId);
            throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
        }

        #endregion

        #region Runtime Permission Validation

        var permissionResult = await _unitOfWork.StoreProcedureRepository
            .CheckInstitutionStaffMemberPermissionAsync(
                institutionId,
                userStaffMemberId,
                tokenRoleId,
                request.DTO.ModuleId,
                request.DTO.OperationId,
                cancellationToken);

        InstitutionRuntimePermissionValidator.EnsureAllowed(permissionResult);

        #endregion

        var deleted = await _unitOfWork.AssetTypeRepository.DeleteAsync(
            request.DTO.Id,
            institutionId,
            userStaffMemberId,
            cancellationToken);
        if (!deleted)
        {
            _logger.LogWarning(
                "Asset Type {AssetTypeId} was not found for institution {InstitutionId}.",
                request.DTO.Id,
                institutionId);
            throw new ApiException("Asset Type not found or already deleted.", 404);
        }

        return ApiResponse<bool>.Success(true, "Asset Type deleted successfully.");
    }

    #endregion
}

#endregion
