// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Soft-deletes institution-owned assets and their images.
// ================================================================

using skolenza.application.DTOS.AssetDTO.asset;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using MediatR;

namespace skolenza.application.Features.AssetFeatures.Assets.Handlers;

#region Command

/// <summary>Represents the request to delete an asset.</summary>
public class DeleteAssetCommand : IRequest<ApiResponse<bool>>
{
    /// <summary>Initializes a new instance of the <see cref="DeleteAssetCommand"/> class.</summary>
    public DeleteAssetCommand(DeleteAssetReqestDTO dto) => DTO = dto;

    /// <summary>Gets the asset selected for deletion.</summary>
    public DeleteAssetReqestDTO DTO { get; }
}

#endregion

#region Handler

/// <summary>Handles soft deletion of institution-owned assets.</summary>
public class DeleteAssetCommandHandler : IRequestHandler<DeleteAssetCommand, ApiResponse<bool>>
{
    #region Fields

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICommonRequestService _commonRequestService;

    #endregion

    #region Constructor

    /// <summary>Initializes a new instance of the <see cref="DeleteAssetCommandHandler"/> class.</summary>
    public DeleteAssetCommandHandler(
        IUnitOfWork unitOfWork,
        ICommonRequestService commonRequestService)
    {
        _unitOfWork = unitOfWork;
        _commonRequestService = commonRequestService;
    }

    #endregion

    #region Handle

    /// <inheritdoc />
    public async Task<ApiResponse<bool>> Handle(
        DeleteAssetCommand request,
        CancellationToken cancellationToken)
    {
        if (request.DTO is null || request.DTO.Id <= 0)
        {
            throw new ValidationErrorException(
                "Invalid Asset Id.",
                new List<string> { "Asset Id must be greater than 0." });
        }

        // Resolve the trusted institution-user context.
        var validation = await _commonRequestService.ValidateInstitutionUserRequestAsync();
        if (!validation.Success)
        {
            throw new UnauthorizedAccessException(validation.ErrorMessage);
        }

        var deleted = await _unitOfWork.AssetRepository.DeleteAssetAsync(
            request.DTO.Id,
            validation.InstitutionId,
            validation.LoggedInStaffMemberId,
            cancellationToken);
        if (!deleted)
        {
            throw new ApiException("Asset not found or already deleted.", 404);
        }

        return ApiResponse<bool>.Success(true, "Asset deleted successfully.");
    }

    #endregion
}

#endregion
