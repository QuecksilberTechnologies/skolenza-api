// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Soft-deletes institution-owned asset categories from authenticated requests.
// ================================================================

using skolenza.application.DTOS.AssetDTO.category;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using MediatR;

namespace skolenza.application.Features.AssetFeatures.Category.Handlers;

#region Command

/// <summary>Represents the request to delete an asset category.</summary>
public class DeleteCategoryCommand : IRequest<ApiResponse<bool>>
{
    /// <summary>Initializes a new instance of the <see cref="DeleteCategoryCommand"/> class.</summary>
    public DeleteCategoryCommand(DeleteCategoryReqestDTO dto) => DTO = dto;

    /// <summary>Gets the asset category selected for deletion.</summary>
    public DeleteCategoryReqestDTO DTO { get; }
}

#endregion

#region Handler

/// <summary>Handles soft deletion of institution-owned asset categories.</summary>
public class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand, ApiResponse<bool>>
{
    #region Fields

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICommonRequestService _commonRequestService;

    #endregion

    #region Constructor

    /// <summary>Initializes a new instance of the <see cref="DeleteCategoryCommandHandler"/> class.</summary>
    public DeleteCategoryCommandHandler(
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
        DeleteCategoryCommand request,
        CancellationToken cancellationToken)
    {
        if (request.DTO is null || request.DTO.Id <= 0)
        {
            throw new ValidationErrorException(
                "Invalid Category Id.",
                new List<string> { "Category Id must be greater than 0." });
        }

        // Resolve the trusted institution-user context.
        var validation = await _commonRequestService.ValidateInstitutionUserRequestAsync();
        if (!validation.Success)
        {
            throw new UnauthorizedAccessException(validation.ErrorMessage);
        }

        var deleted = await _unitOfWork.AssetCategoryRepository.DeleteAsync(
            request.DTO.Id,
            validation.InstitutionId,
            validation.LoggedInStaffMemberId,
            cancellationToken);
        if (!deleted)
        {
            throw new ApiException("Delete failed. Record may not exist or already be deleted.", 404);
        }

        return ApiResponse<bool>.Success(true, "Asset Category deleted successfully.");
    }

    #endregion
}

#endregion
