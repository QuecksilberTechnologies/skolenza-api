// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the Host-side command contract for Institution soft deletion.
// ================================================================

using skolenza.application.DTOs.Institution;
using skolenza.application.Common.Helpers;
using skolenza.application.DTOs.BaseDTO;
using skolenza.application.Constants;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Wrappers;
using MediatR;

namespace skolenza.application.Features.InstitutionManagementCmd.Commands;

#region Command

/// <summary>
/// Represents the Host-side request to place a Institution into the existing soft-delete lifecycle.
/// </summary>
public sealed class DeleteInstitutionCommand : IRequest<ApiResponse<bool>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteInstitutionCommand"/> class.
    /// </summary>
    /// <param name="requestDTO">The Institution deletion request.</param>
    public DeleteInstitutionCommand(DeleteInstitutionRequestDTO requestDTO)
    {
        RequestDTO = requestDTO;
    }

    /// <summary>
    /// Gets the Institution deletion request.
    /// </summary>
    public DeleteInstitutionRequestDTO RequestDTO { get; }
}

#endregion

#region Host-Managed Route Command

/// <summary>
/// Represents the Host-managed route request to soft delete one Institution.
/// </summary>
public sealed class DeleteHostManagedInstitutionCommand : IRequest<ApiResponse<bool>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteHostManagedInstitutionCommand"/> class.
    /// </summary>
    /// <param name="encryptedInstitutionId">The encrypted Institution identifier from the route or request body.</param>
    /// <param name="permissionRequest">The Host module-operation permission metadata.</param>
    public DeleteHostManagedInstitutionCommand(
        string encryptedInstitutionId,
        PermissionRequestDTO? permissionRequest)
    {
        EncryptedInstitutionId = encryptedInstitutionId;
        ModuleId = permissionRequest?.ModuleId ?? 0;
        OperationId = permissionRequest?.OperationId ?? 0;
    }

    /// <summary>
    /// Gets the encrypted Institution identifier from the route or request body.
    /// </summary>
    public string EncryptedInstitutionId { get; }

    /// <summary>
    /// Gets the requested Host module identifier.
    /// </summary>
    public int ModuleId { get; }

    /// <summary>
    /// Gets the requested Host operation identifier.
    /// </summary>
    public int OperationId { get; }
}

#endregion

#region Host-Managed Route Handler

/// <summary>
/// Soft deletes a Host-managed Institution and deactivates related login credentials in one transaction.
/// </summary>
public sealed class DeleteHostManagedInstitutionCommandHandler
    : IRequestHandler<DeleteHostManagedInstitutionCommand, ApiResponse<bool>>
{
    #region Fields

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICommonRequestService _commonRequestService;
    private readonly IIdEncoderService _idEncoderService;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteHostManagedInstitutionCommandHandler"/> class.
    /// </summary>
    /// <param name="unitOfWork">Provides Institution persistence and transaction operations.</param>
    /// <param name="commonRequestService">Validates the current Host principal.</param>
    /// <param name="encryptionService">Decrypts the Host-facing Institution identifier before persistence.</param>
    public DeleteHostManagedInstitutionCommandHandler(
        IUnitOfWork unitOfWork,
        ICommonRequestService commonRequestService,
        IIdEncoderService idEncoderService)
    {
        _unitOfWork = unitOfWork;
        _commonRequestService = commonRequestService;
        _idEncoderService = idEncoderService;
    }

    #endregion

    #region Handle

    /// <summary>
    /// Soft deletes a Institution and deactivates all related login credentials atomically.
    /// </summary>
    /// <param name="request">The Host-managed Institution deletion command.</param>
    /// <param name="cancellationToken">The token used to observe cancellation.</param>
    /// <returns>A successful response when the Institution lifecycle transition completes.</returns>
    /// <exception cref="ValidationErrorException">Thrown when the route identifier is invalid.</exception>
    /// <exception cref="NotFoundException">Thrown when the Institution is unavailable or already soft deleted.</exception>
    public async Task<ApiResponse<bool>> Handle(
        DeleteHostManagedInstitutionCommand request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
        }

        var hostContext = await _commonRequestService.ValidateHostUserPermissionRequestAsync();
        var institutionId = HostInstitutionIdentifierProtector.Decrypt(
            request.EncryptedInstitutionId,
            hostContext.InstitutionEncryptionKey,
            _idEncoderService);

        var institution = await _unitOfWork.InstitutionRepository
            .GetHostManagedInstitutionByIdAsync(institutionId, cancellationToken);

        if (institution is null)
        {
            throw new NotFoundException(AppConstants.ErrorMessages.ResourceNotFound);
        }

        var utcNow = DateTime.UtcNow;
        institution.IsSoftDeleted = true;
        institution.IsActive = false;
        institution.SoftDeletedById = hostContext.HostUserId;
        institution.DeletedDateTime = utcNow;
        institution.UpdatedById = hostContext.HostUserId;
        institution.UpdatedDateTime = utcNow;

        var transactionStarted = false;
        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            transactionStarted = true;

            // Keep the Institution and every related credential state transition atomic.
            await _unitOfWork.InstitutionRepository
            .SoftDeleteInstitutionAndDeactivateCredentialsAsync(institution, hostContext.HostUserId, utcNow, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            transactionStarted = false;
        }
        catch
        {
            if (transactionStarted)
            {
                await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            }

            throw;
        }

        return ApiResponse<bool>.Success(
            true,
            AppConstants.SuccessMessages.InstitutionDeletedSuccessfully);
    }

    #endregion
}

#endregion
