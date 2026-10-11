// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Retrieves institution-owned asset types from authenticated requests.
// ================================================================

using skolenza.application.DTOS.AssetDTO.type;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using MediatR;

namespace skolenza.application.Features.AssetFeatures.Type.Handlers;

#region Query

/// <summary>
/// Represents the request to retrieve asset types.
/// </summary>
public class GetAllTypeCommand : IRequest<ApiResponse<List<GetTypeResponseDTO>>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetAllTypeCommand"/> class.
    /// </summary>
    /// <param name="dto">The client-supplied asset type filters.</param>
    public GetAllTypeCommand(GetTypeRequestDTO dto)
    {
        DTO = dto;
    }

    /// <summary>
    /// Gets the client-supplied asset type filters.
    /// </summary>
    public GetTypeRequestDTO DTO { get; }
}

#endregion

#region Handler

/// <summary>
/// Handles retrieval of institution-owned asset types.
/// </summary>
public class GetAllTypeCommandHandler : IRequestHandler<GetAllTypeCommand, ApiResponse<List<GetTypeResponseDTO>>>
{
    #region Fields

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICommonRequestService _commonRequestService;

    #endregion

    #region Constructor

    /// <summary>
/// Initializes a new instance of the <see cref="GetAllTypeCommandHandler"/> class.
    /// </summary>
    public GetAllTypeCommandHandler(
        IUnitOfWork unitOfWork,
        ICommonRequestService commonRequestService)
    {
        _unitOfWork = unitOfWork;
        _commonRequestService = commonRequestService;
    }

    #endregion

    #region Handle

    /// <inheritdoc />
    public async Task<ApiResponse<List<GetTypeResponseDTO>>> Handle(
        GetAllTypeCommand request,
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

        var pagedResult = await _unitOfWork.AssetTypeRepository.GetAllAsync(
            validation.InstitutionId,
            request.DTO,
            cancellationToken);

        return ApiResponse<List<GetTypeResponseDTO>>.SuccessPaginatedPercentage(
            Data: pagedResult.Data ?? new List<GetTypeResponseDTO>(),
            PageNumber: pagedResult.PageNumber,
            PageSize: pagedResult.PageSize,
            TotalRecords: pagedResult.TotalCount,
            TotalPages: pagedResult.TotalPages,
            Message: pagedResult.TotalCount == 0
                ? "No Asset Types found."
                : "Asset Types fetched successfully.",
            HasUploadedAll: pagedResult.HasUploadedAll,
            CompletionPercentage: pagedResult.CompletionPercentage);
    }

    #endregion
}

#endregion
