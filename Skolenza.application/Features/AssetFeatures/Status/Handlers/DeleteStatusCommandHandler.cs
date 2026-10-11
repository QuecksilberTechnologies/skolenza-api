// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Soft-deletes institution-owned asset statuses from authenticated requests.
// ================================================================

using skolenza.application.DTOS.AssetDTO.status;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using MediatR;

namespace skolenza.application.Features.AssetFeatures.Status.Handlers;

#region Command

/// <summary>
/// Represents the request to delete an asset status.
/// </summary>
public class DeleteStatusCommand : IRequest<ApiResponse<bool>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteStatusCommand"/> class.
    /// </summary>
    public DeleteStatusCommand(DeleteStatusReqestDTO dto)
    {
        DTO = dto;
    }

    /// <summary>
    /// Gets the asset status selected for deletion.
    /// </summary>
    public DeleteStatusReqestDTO DTO { get; }
}

#endregion

#region Handler

/// <summary>
/// Handles soft deletion of institution-owned asset statuses.
/// </summary>
public class DeleteStatusCommandHandler : IRequestHandler<DeleteStatusCommand, ApiResponse<bool>>
{
    #region Fields

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICommonRequestService _commonRequestService;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteStatusCommandHandler"/> class.
    /// </summary>
    public DeleteStatusCommandHandler(
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
        DeleteStatusCommand request,
        CancellationToken cancellationToken)
    {
        if (request.DTO is null || request.DTO.Id <= 0)
        {
            throw new ValidationErrorException(
                "Invalid Status Id.",
                new List<string> { "Status Id must be greater than 0." });
        }

        // Resolve the trusted institution-user context.
        var validation = await _commonRequestService.ValidateInstitutionUserRequestAsync();
        if (!validation.Success)
        {
            throw new UnauthorizedAccessException(validation.ErrorMessage);
        }

        var deleted = await _unitOfWork.AssetStatusRepository.DeleteAsync(
            request.DTO.Id,
            validation.InstitutionId,
            validation.LoggedInStaffMemberId,
            cancellationToken);
        if (!deleted)
        {
            throw new ApiException("Delete failed. Record not found or already deleted.", 404);
        }

        return ApiResponse<bool>.Success(true, "Asset Status deleted successfully.");
    }

    #endregion
}

#endregion
