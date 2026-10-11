// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles the request to update an Operation.
// ================================================================

using AutoMapper;
using skolenza.application.DTOs.Operation;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using MediatR;

namespace skolenza.application.Features.OperationsMasterCmd.Handlers;

#region Command

/// <summary>
/// Represents the request to update an operation.
/// </summary>
public class UpdateOperationCommand
    : IRequest<ApiResponse<List<GetOperationResponseDTO>>>
{
    /// <summary>
    /// Gets the operation details to update.
    /// </summary>
    public UpdateOperationRequestDTO UpdateOperationRequestDTO { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateOperationCommand"/> class.
    /// </summary>
    /// <param name="updateOperationRequestDTO">The operation details to update.</param>
    public UpdateOperationCommand(UpdateOperationRequestDTO updateOperationRequestDTO)
    {
        UpdateOperationRequestDTO = updateOperationRequestDTO;
    }
}

#endregion

#region Handler

/// <summary>
/// Handles the request to update an operation.
/// </summary>
public class UpdateOperationCommandHandler
    : IRequestHandler<UpdateOperationCommand, ApiResponse<List<GetOperationResponseDTO>>>
{
    #region Fields

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICommonRequestService _commonRequestService;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateOperationCommandHandler"/> class.
    /// </summary>
    /// <param name="unitOfWork">The unit of work used to access persistence.</param>
    /// <param name="mapper">The mapper used to translate response entities.</param>
    /// <param name="commonRequestService">The shared validator for the authenticated Host request.</param>
    public UpdateOperationCommandHandler(
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
    /// Updates the supplied operation without overwriting unspecified fields.
    /// </summary>
    /// <param name="request">The update-operation request.</param>
    /// <param name="cancellationToken">The token used to observe cancellation.</param>
    /// <returns>A successful response containing the current operation list.</returns>
    public async Task<ApiResponse<List<GetOperationResponseDTO>>> Handle(
        UpdateOperationCommand request,
        CancellationToken cancellationToken)
    {
        var hostContext =
            await _commonRequestService.ValidateHostSuperAdminRequestAsync();
        var hostUserId = hostContext.HostUserId;
        cancellationToken.ThrowIfCancellationRequested();

        var dto = request?.UpdateOperationRequestDTO
            ?? throw new ValidationErrorException("Operation details are required.");

        if (dto.Id <= 0)
        {
            throw new ValidationErrorException("A valid operation ID is required.");
        }

        if (dto.OperationName is not null && string.IsNullOrWhiteSpace(dto.OperationName))
        {
            throw new ValidationErrorException("Operation name cannot be empty.");
        }

        if (dto.OperationType.HasValue && dto.OperationType <= 0)
        {
            throw new ValidationErrorException("A valid operation type is required.");
        }

        var operation = await _unitOfWork.OperationRepository
            .GetOperationByIdAsync(dto.Id)
            ?? throw new ApiException("Operation not found.", 404);

        if (!string.IsNullOrWhiteSpace(dto.OperationName))
        {
            operation.OperationName = dto.OperationName.Trim();
        }

        if (dto.Remark is not null)
        {
            operation.Remark = dto.Remark;
        }

        if (dto.IconImage is not null)
        {
            operation.IconImage = dto.IconImage;
        }

        if (dto.OperationType.HasValue)
        {
            operation.OperationType = dto.OperationType.Value;
        }

        if (dto.IsActive.HasValue)
        {
            operation.IsActive = dto.IsActive.Value;
        }

        var utcNow = DateTime.UtcNow;
        operation.UpdatedById = hostUserId;
        operation.UpdatedDateTime = utcNow;

        var operations = await _unitOfWork.OperationRepository
            .UpdateOperationAsync(operation);

        var response = _mapper.Map<List<GetOperationResponseDTO>>(operations);

        return ApiResponse<List<GetOperationResponseDTO>>.Success(
            response,
            "Operation updated successfully.");
    }

    #endregion
}

#endregion
