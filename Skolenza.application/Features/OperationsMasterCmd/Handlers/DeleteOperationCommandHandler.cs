// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles the request to delete an Operation.
// ================================================================

using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using MediatR;

namespace skolenza.application.Features.OperationsMasterCmd.Handlers;

#region Command

/// <summary>
/// Represents the request to delete an operation.
/// </summary>
public class DeleteOperationCommand : IRequest<ApiResponse<bool>>
{
    /// <summary>
    /// Gets the ID of the operation to delete.
    /// </summary>
    public int OperationId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteOperationCommand"/> class.
    /// </summary>
    /// <param name="operationId">The ID of the operation to delete.</param>
    public DeleteOperationCommand(int operationId)
    {
        OperationId = operationId;
    }
}

#endregion

#region Handler

/// <summary>
/// Handles the request to delete an operation.
/// </summary>
public class DeleteOperationCommandHandler
    : IRequestHandler<DeleteOperationCommand, ApiResponse<bool>>
{
    #region Fields

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICommonRequestService _commonRequestService;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteOperationCommandHandler"/> class.
    /// </summary>
    /// <param name="unitOfWork">The unit of work used to access persistence.</param>
    /// <param name="commonRequestService">The shared validator for the authenticated Host request.</param>
    public DeleteOperationCommandHandler(
        IUnitOfWork unitOfWork,
        ICommonRequestService commonRequestService)
    {
        _unitOfWork = unitOfWork;
        _commonRequestService = commonRequestService;
    }

    #endregion

    #region Handle

    /// <summary>
    /// Deactivates the requested operation.
    /// </summary>
    /// <param name="request">The delete-operation request.</param>
    /// <param name="cancellationToken">The token used to observe cancellation.</param>
    /// <returns>A successful response when the operation is deleted.</returns>
    public async Task<ApiResponse<bool>> Handle(
        DeleteOperationCommand request,
        CancellationToken cancellationToken)
    {
        var hostContext =
            await _commonRequestService.ValidateHostSuperAdminRequestAsync();
        var hostUserId = hostContext.HostUserId;
        cancellationToken.ThrowIfCancellationRequested();

        if (request is null || request.OperationId <= 0)
        {
            throw new ValidationErrorException("A valid operation ID is required.");
        }

        var operation = await _unitOfWork.OperationRepository
            .GetOperationByIdAsync(request.OperationId)
            ?? throw new ApiException("Operation not found.", 404);

        var utcNow = DateTime.UtcNow;
        operation.UpdatedById = hostUserId;
        operation.UpdatedDateTime = utcNow;

        var isDeleted = await _unitOfWork.OperationRepository
            .DeleteOperationAsync(operation);

        if (!isDeleted)
        {
            throw new ApiException("Operation not found.", 404);
        }

        return ApiResponse<bool>.Success(true, "Operation deleted successfully.");
    }

    #endregion
}

#endregion
