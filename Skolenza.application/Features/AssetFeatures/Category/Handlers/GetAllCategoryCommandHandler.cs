// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Retrieves institution-owned asset categories from authenticated requests.
// ================================================================

using skolenza.application.DTOS.AssetDTO.category;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using MediatR;

namespace skolenza.application.Features.AssetFeatures.Category.Handlers;

#region Query

/// <summary>Represents the request to retrieve asset categories.</summary>
public class GetAllCategoryCommand : IRequest<ApiResponse<List<GetCategoryResponseDTO>>>
{
    /// <summary>Initializes a new instance of the <see cref="GetAllCategoryCommand"/> class.</summary>
    public GetAllCategoryCommand(GetCategoryReqestDTO dto) => DTO = dto;

    /// <summary>Gets the client-supplied asset category filters.</summary>
    public GetCategoryReqestDTO DTO { get; }
}

#endregion

#region Handler

/// <summary>Handles retrieval of institution-owned asset categories.</summary>
public class GetAllCategoryCommandHandler
    : IRequestHandler<GetAllCategoryCommand, ApiResponse<List<GetCategoryResponseDTO>>>
{
    #region Fields

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICommonRequestService _commonRequestService;

    #endregion

    #region Constructor

    /// <summary>Initializes a new instance of the <see cref="GetAllCategoryCommandHandler"/> class.</summary>
    public GetAllCategoryCommandHandler(
        IUnitOfWork unitOfWork,
        ICommonRequestService commonRequestService)
    {
        _unitOfWork = unitOfWork;
        _commonRequestService = commonRequestService;
    }

    #endregion

    #region Handle

    /// <inheritdoc />
    public async Task<ApiResponse<List<GetCategoryResponseDTO>>> Handle(
        GetAllCategoryCommand request,
        CancellationToken cancellationToken)
    {
        if (request.DTO is null)
        {
            throw new ValidationErrorException(
                "Invalid request.",
                new List<string> { "Request DTO is required." });
        }

        // Resolve the trusted institution context separately from client filters.
        var validation = await _commonRequestService.ValidateInstitutionUserRequestAsync();
        if (!validation.Success)
        {
            throw new UnauthorizedAccessException(validation.ErrorMessage);
        }

        var pagedResult = await _unitOfWork.AssetCategoryRepository.GetAllAsync(
            validation.InstitutionId,
            request.DTO,
            cancellationToken);

        return ApiResponse<List<GetCategoryResponseDTO>>.SuccessPaginatedPercentage(
            Data: pagedResult.Data ?? new List<GetCategoryResponseDTO>(),
            PageNumber: pagedResult.PageNumber,
            PageSize: pagedResult.PageSize,
            TotalRecords: pagedResult.TotalCount,
            TotalPages: pagedResult.TotalPages,
            Message: pagedResult.TotalCount == 0
                ? "No Asset Categories found."
                : "Asset Categories fetched successfully.",
            HasUploadedAll: pagedResult.HasUploadedAll,
            CompletionPercentage: pagedResult.CompletionPercentage);
    }

    #endregion
}

#endregion
