// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles the request to retrieve an Operation by ID.
// ================================================================

using AutoMapper;
using skolenza.application.DTOs.Operation;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using MediatR;

namespace skolenza.application.Features.OperationsMasterCmd.Handlers;

#region Query

/// <summary>
/// Represents the read-only request to retrieve an operation by ID.
/// </summary>
public class GetOperationByIdQuery
    : IRequest<ApiResponse<GetOperationResponseDTO>>
{
    /// <summary>
    /// Gets the ID of the operation to retrieve.
    /// </summary>
    public int OperationId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="GetOperationByIdQuery"/> class.
    /// </summary>
    /// <param name="operationId">The ID of the operation to retrieve.</param>
    public GetOperationByIdQuery(int operationId)
    {
        OperationId = operationId;
    }
}

#endregion

#region Handler

/// <summary>
/// Handles the request to retrieve an operation by ID.
/// </summary>
public class GetOperationByIdQueryHandler
    : IRequestHandler<GetOperationByIdQuery, ApiResponse<GetOperationResponseDTO>>
{
    #region Fields

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICommonRequestService _commonRequestService;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="GetOperationByIdQueryHandler"/> class.
    /// </summary>
    /// <param name="unitOfWork">The unit of work used to access persistence.</param>
    /// <param name="mapper">The mapper used to translate the operation entity.</param>
    /// <param name="commonRequestService">The shared validator for the authenticated Host request.</param>
    public GetOperationByIdQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICommonRequestService commonRequestService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _commonRequestService = commonRequestService;
    }

    #endregion

    #region Handle

    /// <summary>
    /// Retrieves the requested operation.
    /// </summary>
    /// <param name="request">The get-operation-by-ID request.</param>
    /// <param name="cancellationToken">The token used to observe cancellation.</param>
    /// <returns>A successful response containing the requested operation.</returns>
    public async Task<ApiResponse<GetOperationResponseDTO>> Handle(
        GetOperationByIdQuery request,
        CancellationToken cancellationToken)
    {
        await _commonRequestService.ValidateHostUserRequestAsync();
        cancellationToken.ThrowIfCancellationRequested();

        if (request is null || request.OperationId <= 0)
        {
            throw new ValidationErrorException("A valid operation ID is required.");
        }

        var operation = await _unitOfWork.OperationRepository
            .GetOperationByIdAsync(request.OperationId)
            ?? throw new ApiException("Operation not found.", 404);

        var response = _mapper.Map<GetOperationResponseDTO>(operation);

        return ApiResponse<GetOperationResponseDTO>.Success(
            response,
            "Operation retrieved successfully.");
    }

    #endregion
}

#endregion
