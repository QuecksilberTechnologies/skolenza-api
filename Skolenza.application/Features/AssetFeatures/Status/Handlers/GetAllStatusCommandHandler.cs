// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Retrieves institution-owned asset statuses from authenticated requests.
// ================================================================

using skolenza.application.DTOS.AssetDTO.status;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using MediatR;

namespace skolenza.application.Features.AssetFeatures.Status.Handlers;

#region Query

/// <summary>
/// Represents the request to retrieve asset statuses.
/// </summary>
public class GetAllAssetStatusCommand : IRequest<ApiResponse<List<GetStatusResponseDTO>>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetAllAssetStatusCommand"/> class.
    /// </summary>
    public GetAllAssetStatusCommand(GetStatusRequestDTO dto)
    {
        DTO = dto;
    }

    /// <summary>
    /// Gets the client-supplied asset status filters.
    /// </summary>
    public GetStatusRequestDTO DTO { get; }
}

#endregion

#region Handler

/// <summary>
/// Handles retrieval of institution-owned asset statuses.
/// </summary>
public class GetAllStatusCommandHandler
    : IRequestHandler<GetAllAssetStatusCommand, ApiResponse<List<GetStatusResponseDTO>>>
{
    #region Fields

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICommonRequestService _commonRequestService;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="GetAllStatusCommandHandler"/> class.
    /// </summary>
    public GetAllStatusCommandHandler(
        IUnitOfWork unitOfWork,
        ICommonRequestService commonRequestService)
    {
        _unitOfWork = unitOfWork;
        _commonRequestService = commonRequestService;
    }

    #endregion

    #region Handle

    /// <inheritdoc />
    public async Task<ApiResponse<List<GetStatusResponseDTO>>> Handle(
        GetAllAssetStatusCommand request,
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

        var pagedResult = await _unitOfWork.AssetStatusRepository.GetAllAsync(
            validation.InstitutionId,
            request.DTO,
            cancellationToken);

        return ApiResponse<List<GetStatusResponseDTO>>.SuccessPaginatedPercentage(
            Data: pagedResult.Data ?? new List<GetStatusResponseDTO>(),
            PageNumber: pagedResult.PageNumber,
            PageSize: pagedResult.PageSize,
            TotalRecords: pagedResult.TotalCount,
            TotalPages: pagedResult.TotalPages,
            Message: pagedResult.TotalCount == 0
                ? "No Asset Status found."
                : "Asset Status fetched successfully.",
            HasUploadedAll: pagedResult.HasUploadedAll,
            CompletionPercentage: pagedResult.CompletionPercentage);
    }

    #endregion
}

#endregion
