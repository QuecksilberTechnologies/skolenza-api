// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles the request to create an Operation.
// ================================================================

using AutoMapper;
using skolenza.application.DTOs.Operation;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;

namespace skolenza.application.Features.OperationsMasterCmd.Handlers;

#region Command

/// <summary>
/// Represents the request to create an operation.
/// </summary>
public class CreateOperationCommand
    : IRequest<ApiResponse<List<GetOperationResponseDTO>>>
{
    /// <summary>
    /// Gets the operation details to create.
    /// </summary>
    public CreateOperationRequestDTO CreateOperationRequestDTO { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateOperationCommand"/> class.
    /// </summary>
    /// <param name="createOperationRequestDTO">The operation details to create.</param>
    public CreateOperationCommand(CreateOperationRequestDTO createOperationRequestDTO)
    {
        CreateOperationRequestDTO = createOperationRequestDTO;
    }
}

#endregion

#region Handler

/// <summary>
/// Handles the request to create an operation.
/// </summary>
public class CreateOperationCommandHandler
    : IRequestHandler<CreateOperationCommand, ApiResponse<List<GetOperationResponseDTO>>>
{
    #region Fields

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICommonRequestService _commonRequestService;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateOperationCommandHandler"/> class.
    /// </summary>
    /// <param name="unitOfWork">The unit of work used to access persistence.</param>
    /// <param name="mapper">The mapper used to translate between DTOs and entities.</param>
    /// <param name="commonRequestService">The shared validator for the authenticated Host request.</param>
    public CreateOperationCommandHandler(
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
    /// Creates an operation from the supplied request details.
    /// </summary>
    /// <param name="request">The create-operation request.</param>
    /// <param name="cancellationToken">The token used to observe cancellation.</param>
    /// <returns>A successful response containing the current operation list.</returns>
    public async Task<ApiResponse<List<GetOperationResponseDTO>>> Handle(
        CreateOperationCommand request,
        CancellationToken cancellationToken)
    {
        var hostUserId = await _commonRequestService.ValidateHostUserRequestAsync();
        cancellationToken.ThrowIfCancellationRequested();

        var dto = request?.CreateOperationRequestDTO
            ?? throw new ValidationErrorException("Operation details are required.");

        if (string.IsNullOrWhiteSpace(dto.OperationName))
        {
            throw new ValidationErrorException("Operation name is required.");
        }

        if (dto.OperationType <= 0)
        {
            throw new ValidationErrorException("A valid operation type is required.");
        }

        var utcNow = DateTime.UtcNow;
        var operation = _mapper.Map<Operation>(dto);
        operation.OperationName = dto.OperationName.Trim();
        operation.AddedById = hostUserId;
        operation.AddedDateTime = utcNow;
        operation.UpdatedById = null;
        operation.UpdatedDateTime = null;

        var operations = await _unitOfWork.OperationRepository
            .CreateOperationAsync(operation);

        var response = _mapper.Map<List<GetOperationResponseDTO>>(operations);

        return ApiResponse<List<GetOperationResponseDTO>>.Success(
            response,
            "Operation created successfully.");
    }

    #endregion
}

#endregion
