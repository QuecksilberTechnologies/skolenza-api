// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Updates institution-owned asset statuses from authenticated requests.
// ================================================================

using AutoMapper;
using skolenza.application.Common.Helpers;
using skolenza.application.Constants;
using skolenza.application.DTOS.AssetDTO.status;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.AssetFeatures.Status.Handlers;

#region Command

/// <summary>
/// Represents the request to update an asset status.
/// </summary>
public class UpdateStatusCommand : IRequest<ApiResponse<bool>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateStatusCommand"/> class.
    /// </summary>
    public UpdateStatusCommand(UpdateStatusRequestDTO dto)
    {
        DTO = dto;
    }

    /// <summary>
    /// Gets the client-supplied update values.
    /// </summary>
    public UpdateStatusRequestDTO DTO { get; }
}

#endregion

#region Handler

/// <summary>
/// Handles updates to institution-owned asset statuses.
/// </summary>
public class UpdateStatusCommandHandler : IRequestHandler<UpdateStatusCommand, ApiResponse<bool>>
{
    #region Fields

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICommonRequestService _commonRequestService;
    private readonly ILogger<UpdateStatusCommandHandler> _logger;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateStatusCommandHandler"/> class.
    /// </summary>
    public UpdateStatusCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICommonRequestService commonRequestService,
        ILogger<UpdateStatusCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _commonRequestService = commonRequestService;
        _logger = logger;
    }

    #endregion

    #region Handle

    /// <inheritdoc />
    public async Task<ApiResponse<bool>> Handle(
        UpdateStatusCommand request,
        CancellationToken cancellationToken)
    {
        if (request.DTO is null || request.DTO.Id <= 0)
        {
            throw new ValidationErrorException(
                "Invalid Status Id.",
                new List<string> { "Status Id must be greater than 0." });
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
                "Invalid Institution authorization context while updating Asset Status. InstitutionId: {InstitutionId}, StaffMemberId: {StaffMemberId}, TokenRoleId: {TokenRoleId}",
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

        // Load the institution-owned entity before applying client changes.
        var entity = await _unitOfWork.AssetStatusRepository.GetByIdForInstitutionAsync(
            request.DTO.Id,
            institutionId,
            cancellationToken);
        if (entity is null)
        {
            throw new ApiException("Asset Status not found or update failed.", 404);
        }

        _mapper.Map(request.DTO, entity);
        entity.UpdatedById = userStaffMemberId;
        entity.UpdatedDateTime = DateTime.UtcNow;

        var updated = await _unitOfWork.AssetStatusRepository.UpdateAsync(entity, cancellationToken);
        if (!updated)
        {
            throw new ApiException("Asset Status not found or update failed.", 404);
        }

        return ApiResponse<bool>.Success(true, "Asset Status updated successfully.");
    }

    #endregion
}

#endregion
