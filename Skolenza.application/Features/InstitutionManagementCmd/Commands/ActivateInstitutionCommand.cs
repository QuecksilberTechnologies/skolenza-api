// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the Host-side command contract for activating a Institution.
// ================================================================

using skolenza.application.DTOs.Institution;
using skolenza.application.Common.Helpers;
using skolenza.application.Constants;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;

namespace skolenza.application.Features.InstitutionManagementCmd.Commands;

#region Command

/// <summary>
/// Represents the Host-side request to activate a Institution.
/// </summary>
public sealed class ActivateInstitutionCommand : IRequest<ApiResponse<HostInstitutionResponseDTO>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ActivateInstitutionCommand"/> class.
    /// </summary>
    /// <param name="requestDTO">The Institution activation request.</param>
    public ActivateInstitutionCommand(ActivateInstitutionRequestDTO requestDTO)
    {
        RequestDTO = requestDTO;
    }

    /// <summary>
    /// Gets the Institution activation request, including its administrative remark.
    /// </summary>
    public ActivateInstitutionRequestDTO RequestDTO { get; }
}

#endregion

#region Handler

/// <summary>
/// Activates a Host-managed Institution and valid, non-soft-deleted Institution login credentials atomically.
/// </summary>
public sealed class ActivateInstitutionCommandHandler
    : IRequestHandler<ActivateInstitutionCommand, ApiResponse<HostInstitutionResponseDTO>>
{
    #region Fields

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICommonRequestService _commonRequestService;
    private readonly IIdEncoderService _idEncoderService;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="ActivateInstitutionCommandHandler"/> class.
    /// </summary>
    /// <param name="unitOfWork">Provides Institution persistence and transaction operations.</param>
    /// <param name="commonRequestService">Validates the current Host principal.</param>
    /// <param name="encryptionService">Protects Institution identifiers at the Host API boundary.</param>
    public ActivateInstitutionCommandHandler(
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
    /// Activates a Institution and its valid credentials in one transaction.
    /// </summary>
    /// <param name="request">The Institution activation command.</param>
    /// <param name="cancellationToken">The token used to observe cancellation.</param>
    /// <returns>The activated Institution response.</returns>
    /// <exception cref="ValidationErrorException">Thrown when the activation request is invalid.</exception>
    /// <exception cref="NotFoundException">Thrown when the Institution is unavailable or soft deleted.</exception>
    public async Task<ApiResponse<HostInstitutionResponseDTO>> Handle(
        ActivateInstitutionCommand request,
        CancellationToken cancellationToken)
    {
        if (request?.RequestDTO is null)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
        }

        var hostContext = await _commonRequestService.ValidateHostUserPermissionRequestAsync();
        var institutionId = HostInstitutionIdentifierProtector.Decrypt(
            request.RequestDTO.InstitutionId,
            hostContext.InstitutionEncryptionKey,
            _idEncoderService);

        var institution = await _unitOfWork.InstitutionRepository
            .GetHostManagedInstitutionByIdAsync(institutionId, cancellationToken);

        if (institution is null)
        {
            throw new NotFoundException(AppConstants.ErrorMessages.ResourceNotFound);
        }

        var utcNow = DateTime.UtcNow;
        institution.IsActive = true;
        institution.UpdatedById = hostContext.HostUserId;
        institution.UpdatedDateTime = utcNow;

        await PersistStatusAsync(institution, hostContext.HostUserId, utcNow, cancellationToken);

        return ApiResponse<HostInstitutionResponseDTO>.Success(
            MapInstitution(institution, hostContext.InstitutionEncryptionKey),
            AppConstants.SuccessMessages.InstitutionActivatedSuccessfully);
    }

    #endregion

    #region Helpers

    private async Task PersistStatusAsync(
        Institution institution,
        long hostUserId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var transactionStarted = false;
        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            transactionStarted = true;
            await _unitOfWork.InstitutionRepository
                .SynchronizeInstitutionStatusAsync(institution, hostUserId, utcNow, cancellationToken);
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
    }

    private HostInstitutionResponseDTO MapInstitution(Institution institution, string institutionEncryptionKey)
    {
        return new HostInstitutionResponseDTO
        {
            Id = HostInstitutionIdentifierProtector.Encrypt(institution.Id, institutionEncryptionKey, _idEncoderService),
            CompanyName = institution.CompanyName,
            InstitutionCode = institution.InstitutionCode,
            CompanyEmailDomain = institution.CompanyEmailDomain,
            InstitutionEmail = institution.InstitutionEmail,
            ContactPersonName = institution.ContactPersonName,
            ContactNumber = institution.ContactNumber,
            CountryId = institution.CountryId,
            IsVerified = institution.IsVerified,
            IsActive = institution.IsActive
        };
    }

    #endregion
}

#endregion
