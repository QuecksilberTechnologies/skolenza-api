// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles the request to retrieve all Operations.
// ================================================================

using AutoMapper;
using skolenza.application.DTOs.Operation;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using MediatR;

namespace skolenza.application.Features.OperationsMasterCmd.Handlers;

#region Query

/// <summary>
/// Represents the read-only request to retrieve all operations.
/// </summary>
public class GetAllOperationsQuery
    : IRequest<ApiResponse<List<GetOperationResponseDTO>>>
{
}

#endregion

#region Handler

/// <summary>
/// Handles the request to retrieve all operations.
/// </summary>
public class GetAllOperationsQueryHandler
    : IRequestHandler<GetAllOperationsQuery, ApiResponse<List<GetOperationResponseDTO>>>
{
    #region Fields

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICommonRequestService _commonRequestService;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="GetAllOperationsQueryHandler"/> class.
    /// </summary>
    /// <param name="unitOfWork">The unit of work used to access persistence.</param>
    /// <param name="mapper">The mapper used to translate operation entities.</param>
    /// <param name="commonRequestService">The shared validator for the authenticated Host request.</param>
    public GetAllOperationsQueryHandler(
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
    /// Retrieves all operations, including an empty result set when no records exist.
    /// </summary>
    /// <param name="request">The get-all-operations request.</param>
    /// <param name="cancellationToken">The token used to observe cancellation.</param>
    /// <returns>A successful response containing all operations.</returns>
    public async Task<ApiResponse<List<GetOperationResponseDTO>>> Handle(
        GetAllOperationsQuery request,
        CancellationToken cancellationToken)
    {
        await _commonRequestService.ValidateHostUserRequestAsync();
        cancellationToken.ThrowIfCancellationRequested();

        var operations = await _unitOfWork.OperationRepository
            .GetAllOperationAsync();

        var response = _mapper.Map<List<GetOperationResponseDTO>>(operations);

        return ApiResponse<List<GetOperationResponseDTO>>.Success(
            response,
            "Operations retrieved successfully.");
    }

    #endregion
}

#endregion
