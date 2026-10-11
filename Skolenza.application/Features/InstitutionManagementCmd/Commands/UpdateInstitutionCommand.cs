// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the Host-side command contract for updating editable Institution fields.
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
using skolenza.domain.Entity;
using MediatR;

namespace skolenza.application.Features.InstitutionManagementCmd.Commands;

#region Command

/// <summary>
/// Represents the Host-side request to update editable Institution fields.
/// </summary>
public sealed class UpdateInstitutionCommand : IRequest<ApiResponse<HostInstitutionResponseDTO>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateInstitutionCommand"/> class.
    /// </summary>
    /// <param name="requestDTO">The Institution update request.</param>
    public UpdateInstitutionCommand(UpdateInstitutionRequestDTO requestDTO)
    {
        RequestDTO = requestDTO;
    }

    /// <summary>
    /// Gets the Institution update request.
    /// </summary>
    public UpdateInstitutionRequestDTO RequestDTO { get; }
}

#endregion

#region Host-Managed Route Command

/// <summary>
/// Represents the Host-managed route request to update Institution details using an authoritative route identifier.
/// </summary>
public sealed class UpdateHostManagedInstitutionCommand : IRequest<ApiResponse<HostInstitutionResponseDTO>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateHostManagedInstitutionCommand"/> class.
    /// </summary>
    /// <param name="encryptedInstitutionId">The encrypted Institution identifier from the route.</param>
    /// <param name="requestDTO">The client-editable Institution details.</param>
    /// <param name="permissionRequest">The query-bound Host module-operation permission request.</param>
    public UpdateHostManagedInstitutionCommand(
        string encryptedInstitutionId,
        UpdateHostManagedInstitutionRequestDTO? requestDTO,
        PermissionRequestDTO? permissionRequest)
    {
        EncryptedInstitutionId = encryptedInstitutionId;
        RequestDTO = requestDTO;
        ModuleId = permissionRequest?.ModuleId ?? 0;
        OperationId = permissionRequest?.OperationId ?? 0;
    }

    /// <summary>
    /// Gets the encrypted Institution identifier from the route.
    /// </summary>
    public string EncryptedInstitutionId { get; }

    /// <summary>
    /// Gets the client-editable Institution details.
    /// </summary>
    public UpdateHostManagedInstitutionRequestDTO? RequestDTO { get; }

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
/// Validates and updates Host-managed Institution information using trusted audit context.
/// </summary>
public sealed class UpdateHostManagedInstitutionCommandHandler
    : IRequestHandler<UpdateHostManagedInstitutionCommand, ApiResponse<HostInstitutionResponseDTO>>
{
    #region Fields

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICommonRequestService _commonRequestService;
    private readonly IIdEncoderService _idEncoderService;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateHostManagedInstitutionCommandHandler"/> class.
    /// </summary>
    /// <param name="unitOfWork">Provides Institution persistence and transaction operations.</param>
    /// <param name="commonRequestService">Validates the current Host principal.</param>
    /// <param name="encryptionService">Protects Institution identifiers at the Host API boundary.</param>
    public UpdateHostManagedInstitutionCommandHandler(
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
    /// Updates editable Institution fields while preserving lifecycle and verification state.
    /// </summary>
    /// <param name="request">The Host-managed Institution update command.</param>
    /// <param name="cancellationToken">The token used to observe cancellation.</param>
    /// <returns>The updated Institution response.</returns>
    /// <exception cref="ValidationErrorException">Thrown when the route identifier or request is invalid.</exception>
    /// <exception cref="NotFoundException">Thrown when the Institution is unavailable or soft deleted.</exception>
    /// <exception cref="ConflictException">Thrown when another active Institution owns the submitted email or code.</exception>
    public async Task<ApiResponse<HostInstitutionResponseDTO>> Handle(
        UpdateHostManagedInstitutionCommand request,
        CancellationToken cancellationToken)
    {
        if (request?.RequestDTO is null)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
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

        var dto = request.RequestDTO;
        var submittedEmail = dto.InstitutionEmail?.Trim();
        var submittedCode = dto.InstitutionCode?.Trim();
        var originalCompanyName = institution.CompanyName;
        var countryChanged = dto.CountryId > 0 && institution.CountryId != dto.CountryId;

        if (!string.IsNullOrWhiteSpace(submittedEmail) &&
            !string.Equals(submittedEmail, institution.InstitutionEmail, StringComparison.OrdinalIgnoreCase) &&
            await _unitOfWork.InstitutionRepository.IsInstitutionEmailInUseAsync(submittedEmail, institution.Id, cancellationToken))
        {
            throw new ConflictException(AppConstants.ErrorMessages.ResourceConflict);
        }

        if (!string.IsNullOrWhiteSpace(submittedCode) &&
            !string.Equals(submittedCode, institution.InstitutionCode, StringComparison.OrdinalIgnoreCase) &&
            await _unitOfWork.InstitutionRepository.IsInstitutionCodeInUseAsync(submittedCode, institution.Id, cancellationToken))
        {
            throw new ConflictException(AppConstants.ErrorMessages.ResourceConflict);
        }

        // Preserve omitted optional fields and all server-controlled verification and lifecycle values.
        if (dto.InstitutionTypeId > 0)
        {
            institution.InstitutionTypeId = dto.InstitutionTypeId;
        }

        if (!string.IsNullOrWhiteSpace(dto.CompanyName))
        {
            institution.CompanyName = dto.CompanyName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(submittedCode))
        {
            institution.InstitutionCode = submittedCode;
        }

        if (!string.IsNullOrWhiteSpace(dto.CompanyEmailDomain))
        {
            institution.CompanyEmailDomain = dto.CompanyEmailDomain.Trim();
        }

        if (!string.IsNullOrWhiteSpace(submittedEmail))
        {
            institution.InstitutionEmail = submittedEmail;
        }

        if (dto.ContactPersonName is not null)
        {
            institution.ContactPersonName = dto.ContactPersonName.Trim();
        }

        if (dto.GenderId.HasValue)
        {
            institution.GenderId = dto.GenderId;
        }

        if (dto.ContactNumber is not null)
        {
            institution.ContactNumber = dto.ContactNumber.Trim();
        }

        if (dto.CountryId > 0)
        {
            institution.CountryId = dto.CountryId;
        }

        if (dto.DefaultCurrency.HasValue)
        {
            institution.DefaultCurrency = dto.DefaultCurrency;
        }

        institution.UpdatedById = hostContext.HostUserId;
        institution.UpdatedDateTime = DateTime.UtcNow;

        await _unitOfWork.InstitutionRepository.StageHostManagedUpdateAsync(institution, cancellationToken);
        await SynchronizeInitialLocationAsync(
            institution,
            originalCompanyName,
            countryChanged,
            hostContext.HostUserId,
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<HostInstitutionResponseDTO>.Success(
            MapInstitution(institution, hostContext.InstitutionEncryptionKey),
            AppConstants.SuccessMessages.InstitutionUpdatedSuccessfully);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Keeps the canonical initial location aligned with Host-managed Institution changes.
    /// Legacy Institutions without a location receive the same minimum valid initial row as self-registration.
    /// </summary>
    private async Task SynchronizeInitialLocationAsync(
        Institution institution,
        string originalCompanyName,
        bool countryChanged,
        long actorId,
        CancellationToken cancellationToken)
    {
        var location = await _unitOfWork.CampusRepository
            .GetInitialForUpdateAsync(institution.Id, cancellationToken);

        if (location is null)
        {
            await _unitOfWork.CampusRepository.AddAsync(new Campus
            {
                InstitutionId = institution.Id,
                LocationCode = ConstantValues.InitialCampusCode,
                LocationName = institution.CompanyName,
                LocationType = (short)CampusType.HeadOffice,
                CountryId = institution.CountryId,
                TimeZoneId = ConstantValues.InitialCampusTimeZoneId,
                IsHeadOffice = true,
                IsAttendanceAllowed = false,
                IsActive = true,
                IsSoftDeleted = false,
                AddedById = actorId,
                AddedDateTime = DateTime.UtcNow
            }, cancellationToken);
            return;
        }

        var changed = false;
        if (countryChanged)
        {
            location.CountryId = institution.CountryId;
            location.StateId = null;
            location.DistrictId = null;
            location.LocalityId = null;
            changed = true;
        }

        if (!string.Equals(originalCompanyName, institution.CompanyName, StringComparison.Ordinal) &&
            string.Equals(location.LocationName, originalCompanyName, StringComparison.Ordinal))
        {
            location.LocationName = institution.CompanyName;
            changed = true;
        }

        if (changed)
        {
            location.UpdatedById = actorId;
            location.UpdatedDateTime = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Maps a persisted Institution to the established client response model.
    /// </summary>
    /// <param name="institution">The Institution to map.</param>
    /// <returns>The mapped Institution response.</returns>
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
