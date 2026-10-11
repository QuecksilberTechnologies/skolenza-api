// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Updates institution-owned asset types from authenticated requests.
// ================================================================

using AutoMapper;
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
/// Represents the request to update an asset type.
/// </summary>
public class UpdateAssetTypeCommand : IRequest<ApiResponse<bool>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateAssetTypeCommand"/> class.
    /// </summary>
    /// <param name="dto">The client-supplied update values.</param>
    public UpdateAssetTypeCommand(UpdateTypeRequestDTO dto)
    {
        DTO = dto;
    }

    /// <summary>
    /// Gets the client-supplied update values.
    /// </summary>
    public UpdateTypeRequestDTO DTO { get; }
}

#endregion

#region Handler

/// <summary>
/// Handles updates to institution-owned asset types.
/// </summary>
public class UpdateAssetTypeCommandHandler : IRequestHandler<UpdateAssetTypeCommand, ApiResponse<bool>>
{
    #region Fields

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateAssetTypeCommandHandler> _logger;
    private readonly ICommonRequestService _commonRequestService;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateAssetTypeCommandHandler"/> class.
    /// </summary>
    public UpdateAssetTypeCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<UpdateAssetTypeCommandHandler> logger,
        ICommonRequestService commonRequestService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _commonRequestService = commonRequestService;
    }

    #endregion

    #region Handle

    /// <inheritdoc />
    public async Task<ApiResponse<bool>> Handle(
        UpdateAssetTypeCommand request,
        CancellationToken cancellationToken)
    {
        if (request.DTO is null || request.DTO.Id <= 0)
        {
            throw new ValidationErrorException(
                "Invalid Asset Type Id.",
                new List<string> { "Type Id must be greater than 0." });
        }

        if (string.IsNullOrWhiteSpace(request.DTO.TypeName))
        {
            throw new ValidationErrorException(
                "Type Name is required.",
                new List<string> { "TypeName cannot be empty." });
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
                "Invalid Institution authorization context while updating Asset Type. InstitutionId: {InstitutionId}, StaffMemberId: {StaffMemberId}, TokenRoleId: {TokenRoleId}",
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
        var entity = await _unitOfWork.AssetTypeRepository.GetByIdForInstitutionAsync(
            request.DTO.Id,
            institutionId,
            cancellationToken);
        if (entity is null)
        {
            throw new ApiException("Asset Type not found or update failed.", 404);
        }

        // Map client-editable values and apply the server-controlled audit values.
        _mapper.Map(request.DTO, entity);
        entity.UpdatedById = userStaffMemberId;
        entity.UpdatedDateTime = DateTime.UtcNow;

        var updated = await _unitOfWork.AssetTypeRepository.UpdateAsync(entity, cancellationToken);
        if (!updated)
        {
            _logger.LogWarning("No changes were saved for Asset Type {AssetTypeId}.", entity.Id);
            throw new ApiException("Asset Type not found or update failed.", 404);
        }

        return ApiResponse<bool>.Success(true, "Asset Type updated successfully.");
    }

    #endregion
}

#endregion
