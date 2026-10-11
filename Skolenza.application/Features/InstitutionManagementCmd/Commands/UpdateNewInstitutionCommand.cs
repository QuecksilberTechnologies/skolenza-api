// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Updates a Host-managed Institution and its selected configuration records atomically.
// ================================================================

using System.Text.RegularExpressions;
using skolenza.application.Common.Helpers;
using skolenza.application.Constants;
using skolenza.application.DTOs.BaseDTO;
using skolenza.application.DTOs.Institution;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;

namespace skolenza.application.Features.InstitutionManagementCmd.Commands;

/// <summary>
/// Represents the Host-managed request to update a Institution and its selected configuration records using an encrypted route identifier.
/// </summary>
public sealed class UpdateNewInstitutionCommand : IRequest<ApiResponse<HostInstitutionResponseDTO>>
{
    /// <summary>Initializes a new instance of the <see cref="UpdateNewInstitutionCommand"/> class.</summary>
    public UpdateNewInstitutionCommand(
        string encryptedInstitutionId,
        NewInstitutionUpdateRequestDTO? requestDTO,
        PermissionRequestDTO? permissionRequest)
    {
        EncryptedInstitutionId = encryptedInstitutionId;
        RequestDTO = requestDTO;
        ModuleId = permissionRequest?.ModuleId ?? 0;
        OperationId = permissionRequest?.OperationId ?? 0;
    }

    /// <summary>Gets the encrypted Institution identifier from the route.</summary>
    public string EncryptedInstitutionId { get; }

    /// <summary>Gets the submitted Institution aggregate update request.</summary>
    public NewInstitutionUpdateRequestDTO? RequestDTO { get; }

    /// <summary>Gets the requested Host module identifier.</summary>
    public int ModuleId { get; }

    /// <summary>Gets the requested Host operation identifier.</summary>
    public int OperationId { get; }
}

/// <summary>
/// Handles the atomic Host-side update of a Institution, its profile, one selected location, active email configuration, and active staffMember-code pattern.
/// </summary>
public sealed class UpdateNewInstitutionCommandHandler
    : IRequestHandler<UpdateNewInstitutionCommand, ApiResponse<HostInstitutionResponseDTO>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICommonRequestService _commonRequestService;
    private readonly IIdEncoderService _idEncoderService;

    /// <summary>Initializes a new instance of the <see cref="UpdateNewInstitutionCommandHandler"/> class.</summary>
    public UpdateNewInstitutionCommandHandler(
        IUnitOfWork unitOfWork,
        ICommonRequestService commonRequestService,
        IIdEncoderService idEncoderService)
    {
        _unitOfWork = unitOfWork;
        _commonRequestService = commonRequestService;
        _idEncoderService = idEncoderService;
    }

    /// <summary>Validates Host access and updates the requested Institution aggregate in one database transaction.</summary>
    public async Task<ApiResponse<HostInstitutionResponseDTO>> Handle(
        UpdateNewInstitutionCommand request,
        CancellationToken cancellationToken)
    {
        if (request?.RequestDTO is null)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        }

        var dto = request.RequestDTO;
        ValidateNestedRequest(dto);

        // Runtime permission is enforced centrally before this handler; resolve
        // only the trusted Host context required for auditing and ID protection.
        var hostContext = await _commonRequestService.ValidateHostUserPermissionRequestAsync();
        var institutionId = HostInstitutionIdentifierProtector.Decrypt(
            request.EncryptedInstitutionId,
            hostContext.InstitutionEncryptionKey,
            _idEncoderService);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var institution = await _unitOfWork.InstitutionRepository
                .GetHostManagedInstitutionByIdAsync(institutionId, cancellationToken)
                ?? throw new NotFoundException(AppConstants.ErrorMessages.ResourceNotFound);
            var profile = await _unitOfWork.InstitutionRepository
                .GetInstitutionProfileForUpdateAsync(institutionId, cancellationToken)
                ?? throw new NotFoundException(AppConstants.ErrorMessages.ResourceNotFound);
            var selectedLocation = await _unitOfWork.CampusRepository
                .GetForUpdateAsync(institutionId, dto.SelectedLocation!.CampusId, cancellationToken)
                ?? throw new NotFoundException(AppConstants.ErrorMessages.ResourceNotFound);
            var staffMemberCodePattern = await _unitOfWork.InstitutionStaffCodePatternRepository
                .GetActivePatternForUpdateAsync(institutionId, cancellationToken)
                ?? throw new NotFoundException(AppConstants.ErrorMessages.ResourceNotFound);
            var emailConfiguration = await _unitOfWork.InstitutionEmailConfigRepository
                .GetActiveEmailConfigAsync(institutionId)
                ?? throw new NotFoundException(AppConstants.ErrorMessages.ResourceNotFound);

            var submittedEmail = dto.InstitutionEmail?.Trim();
            var submittedCode = dto.InstitutionCode?.Trim();

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

            var utcNow = DateTime.UtcNow;
            var institutionChanged = ApplyInstitution(dto, institution, submittedEmail, submittedCode);
            ApplyProfile(dto.Profile!, profile);
            var locationChanged = await ApplySelectedLocationAsync(
                dto.SelectedLocation!,
                selectedLocation,
                institutionId,
                institution.CountryId,
                cancellationToken);
            var staffMemberCodePatternChanged = ApplyStaffMemberCodePattern(dto.StaffMemberCodePattern!, staffMemberCodePattern);
            var emailConfigurationChanged = ApplyEmailConfiguration(dto.EmailConfiguration!, emailConfiguration, out var emailConfigurationUpdate);

            if (institutionChanged)
            {
                institution.UpdatedById = hostContext.HostUserId;
                institution.UpdatedDateTime = utcNow;
                await _unitOfWork.InstitutionRepository.StageHostManagedUpdateAsync(institution, cancellationToken);
            }

            if (locationChanged)
            {
                selectedLocation.UpdatedById = hostContext.HostUserId;
                selectedLocation.UpdatedDateTime = utcNow;
            }

            if (staffMemberCodePatternChanged)
            {
                // LastUsedNumber deliberately remains untouched, preserving the issued staffMember-code sequence.
                staffMemberCodePattern.UpdatedById = hostContext.HostUserId;
                staffMemberCodePattern.UpdatedDateTime = utcNow;
                await _unitOfWork.InstitutionStaffCodePatternRepository.UpdatePatternAsync(staffMemberCodePattern);
            }

            if (emailConfigurationChanged)
            {
                // The repository preserves SecrateKey; it is never accepted or assigned by this update flow.
                await _unitOfWork.InstitutionEmailConfigRepository.UpdateEmailConfigAsync(emailConfigurationUpdate);
            }

            // Profile and selected-location lookups are tracked. EF persists only their detected changes.
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return ApiResponse<HostInstitutionResponseDTO>.Success(
                MapInstitution(institution, hostContext.InstitutionEncryptionKey),
                AppConstants.SuccessMessages.InstitutionUpdatedSuccessfully);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    private static void ValidateNestedRequest(NewInstitutionUpdateRequestDTO dto)
    {
        if (dto.Profile is null ||
            dto.SelectedLocation is null ||
            dto.SelectedLocation.CampusId <= 0 ||
            dto.StaffMemberCodePattern is null ||
            dto.EmailConfiguration is null)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        }
    }

    private static bool ApplyInstitution(
        NewInstitutionUpdateRequestDTO dto,
        Institution institution,
        string? submittedEmail,
        string? submittedCode)
    {
        var changed = false;

        if (dto.InstitutionTypeId > 0 && institution.InstitutionTypeId != dto.InstitutionTypeId)
        {
            institution.InstitutionTypeId = dto.InstitutionTypeId;
            changed = true;
        }

        changed |= AssignTrimmedIfProvided(dto.CompanyName, institution.CompanyName, value => institution.CompanyName = value);
        changed |= AssignTrimmedIfProvided(submittedCode, institution.InstitutionCode, value => institution.InstitutionCode = value);
        changed |= AssignTrimmedIfProvided(dto.CompanyEmailDomain, institution.CompanyEmailDomain, value => institution.CompanyEmailDomain = value);
        changed |= AssignTrimmedIfProvided(submittedEmail, institution.InstitutionEmail, value => institution.InstitutionEmail = value);

        if (dto.ContactPersonName is not null)
        {
            changed |= AssignValue(dto.ContactPersonName.Trim(), institution.ContactPersonName, value => institution.ContactPersonName = value);
        }

        if (dto.GenderId.HasValue && institution.GenderId != dto.GenderId)
        {
            institution.GenderId = dto.GenderId;
            changed = true;
        }

        if (dto.ContactNumber is not null)
        {
            changed |= AssignValue(dto.ContactNumber.Trim(), institution.ContactNumber, value => institution.ContactNumber = value);
        }

        if (dto.CountryId > 0 && institution.CountryId != dto.CountryId)
        {
            institution.CountryId = dto.CountryId;
            changed = true;
        }

        if (dto.DefaultCurrency.HasValue && institution.DefaultCurrency != dto.DefaultCurrency)
        {
            institution.DefaultCurrency = dto.DefaultCurrency;
            changed = true;
        }

        return changed;
    }

    private static bool ApplyProfile(NewInstitutionProfileUpdateRequestDTO dto, InstitutionProfile profile)
    {
        var changed = false;
        if (dto.LogoUrl is not null) changed |= AssignValue(dto.LogoUrl.Trim(), profile.LogoUrl, value => profile.LogoUrl = value);
        if (dto.ThemeColor is not null) changed |= AssignValue(dto.ThemeColor.Trim(), profile.ThemeColor, value => profile.ThemeColor = value);
        if (dto.BusinessType is not null)
        {
            if (!AppConstants.IsSupportedInstitutionCategory(dto.BusinessType))
            {
                throw new ValidationErrorException("Select a valid Institution ownership category.");
            }

            changed |= AssignValue(dto.BusinessType.Trim(), profile.BusinessType, value => profile.BusinessType = value);
        }
        if (dto.Industry is not null) changed |= AssignValue(dto.Industry.Trim(), profile.Industry, value => profile.Industry = value);
        if (dto.TotalStaffMembers.HasValue && profile.TotalStaffMembers != dto.TotalStaffMembers) { profile.TotalStaffMembers = dto.TotalStaffMembers; changed = true; }
        if (dto.TotalCampuses.HasValue && profile.TotalCampuses != dto.TotalCampuses) { profile.TotalCampuses = dto.TotalCampuses; changed = true; }
        if (dto.FoundedYear.HasValue && profile.FoundedYear != dto.FoundedYear) { profile.FoundedYear = dto.FoundedYear; changed = true; }
        if (dto.WebsiteUrl is not null) changed |= AssignValue(dto.WebsiteUrl.Trim(), profile.WebsiteUrl, value => profile.WebsiteUrl = value);
        return changed;
    }

    private async Task<bool> ApplySelectedLocationAsync(
        NewInstitutionSelectedLocationUpdateRequestDTO dto,
        Campus location,
        long institutionId,
        int institutionCountryId,
        CancellationToken cancellationToken)
    {
        if (dto.LocationType.HasValue &&
            !AppConstants.IsSupportedInstitutionCampusType((short)dto.LocationType.Value))
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        }

        var locationCode = dto.LocationCode is null ? null : RequireNonEmpty(dto.LocationCode, nameof(dto.LocationCode));
        var locationName = dto.LocationName is null ? null : RequireNonEmpty(dto.LocationName, nameof(dto.LocationName));
        var timeZoneId = dto.TimeZoneId is null ? null : RequireNonEmpty(dto.TimeZoneId, nameof(dto.TimeZoneId));
        if (dto.CountryId is <= 0)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        }

        var finalCountryId = dto.CountryId ?? institutionCountryId;
        var countryChanged = finalCountryId != location.CountryId;
        var finalStateId = countryChanged ? dto.StateId : dto.StateId ?? location.StateId;
        var finalDistrictId = countryChanged ? dto.DistrictId : dto.DistrictId ?? location.DistrictId;
        var finalLocalityId = countryChanged ? dto.LocalityId : dto.LocalityId ?? location.LocalityId;
        if ((countryChanged || dto.StateId.HasValue || dto.DistrictId.HasValue || dto.LocalityId.HasValue) &&
            !await _unitOfWork.CampusRepository.IsValidGeographyAsync(
                finalCountryId,
                finalStateId,
                finalDistrictId,
                finalLocalityId,
                cancellationToken))
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidInstitutionConfigurationReference);
        }

        if (locationCode is not null &&
            !string.Equals(locationCode, location.LocationCode, StringComparison.OrdinalIgnoreCase) &&
            await _unitOfWork.CampusRepository.LocationCodeExistsAsync(
                institutionId,
                locationCode,
                location.Id,
                cancellationToken))
        {
            throw new ConflictException(AppConstants.ErrorMessages.DuplicateCampusCode);
        }

        var changed = false;
        if (locationCode is not null) changed |= AssignValue(locationCode, location.LocationCode, value => location.LocationCode = value);
        if (locationName is not null) changed |= AssignValue(locationName, location.LocationName, value => location.LocationName = value);
        if (dto.LocationType.HasValue && location.LocationType != (short)dto.LocationType.Value) { location.LocationType = (short)dto.LocationType.Value; changed = true; }
        if (location.CountryId != finalCountryId) { location.CountryId = finalCountryId; changed = true; }
        if (location.StateId != finalStateId) { location.StateId = finalStateId; changed = true; }
        if (location.DistrictId != finalDistrictId) { location.DistrictId = finalDistrictId; changed = true; }
        if (location.LocalityId != finalLocalityId) { location.LocalityId = finalLocalityId; changed = true; }
        if (dto.Address is not null) changed |= AssignValue(dto.Address.Trim(), location.Address, value => location.Address = value);
        if (dto.Landmark is not null) changed |= AssignValue(dto.Landmark.Trim(), location.Landmark, value => location.Landmark = value);
        if (dto.PostalCode is not null) changed |= AssignValue(dto.PostalCode.Trim(), location.PostalCode, value => location.PostalCode = value);
        if (dto.Latitude.HasValue && location.Latitude != dto.Latitude) { location.Latitude = dto.Latitude; changed = true; }
        if (dto.Longitude.HasValue && location.Longitude != dto.Longitude) { location.Longitude = dto.Longitude; changed = true; }
        if (dto.GeoFenceRadiusMeters.HasValue && location.GeoFenceRadiusMeters != dto.GeoFenceRadiusMeters) { location.GeoFenceRadiusMeters = dto.GeoFenceRadiusMeters; changed = true; }
        if (timeZoneId is not null) changed |= AssignValue(timeZoneId, location.TimeZoneId, value => location.TimeZoneId = value);
        if (dto.IsGeoFenceEnabled.HasValue && location.IsGeoFenceEnabled != dto.IsGeoFenceEnabled.Value) { location.IsGeoFenceEnabled = dto.IsGeoFenceEnabled.Value; changed = true; }
        if (dto.IsAttendanceAllowed.HasValue && location.IsAttendanceAllowed != dto.IsAttendanceAllowed.Value) { location.IsAttendanceAllowed = dto.IsAttendanceAllowed.Value; changed = true; }
        if (dto.IsBiometricEnabled.HasValue && location.IsBiometricEnabled != dto.IsBiometricEnabled.Value) { location.IsBiometricEnabled = dto.IsBiometricEnabled.Value; changed = true; }
        return changed;
    }

    private static bool ApplyStaffMemberCodePattern(
        NewInstitutionStaffMemberCodePatternUpdateRequestDTO dto,
        StaffMemberCodePattern pattern)
    {
        // Aggregate edits cannot bypass the approved all-staffMember code preview.
        // Validate against a copy so even rejected requests leave tracked values intact.
        pattern = new StaffMemberCodePattern
        {
            Prefix = pattern.Prefix,
            Separator = pattern.Separator,
            RunningNumberLength = pattern.RunningNumberLength,
            IncludeYear = pattern.IncludeYear,
            IncludeMonth = pattern.IncludeMonth,
            IncludeDepartment = pattern.IncludeDepartment
        };
        var changed = false;
        if (dto.Prefix is not null)
        {
            var prefix = RequireNonEmpty(dto.Prefix, nameof(dto.Prefix)).ToUpperInvariant();
            if (prefix.Length > 10 || !Regex.IsMatch(prefix, "^[A-Z]+$"))
            {
                throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
            }

            changed |= AssignValue(prefix, pattern.Prefix, value => pattern.Prefix = value);
        }

        if (dto.Separator is not null)
        {
            var separator = RequireNonEmpty(dto.Separator, nameof(dto.Separator));
            if (separator is not ("_" or "/" or "-"))
            {
                throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
            }

            changed |= AssignValue(separator, pattern.Separator, value => pattern.Separator = value);
        }

        if (dto.RunningNumberLength is not null)
        {
            if (!int.TryParse(dto.RunningNumberLength, out var runningNumberLength) ||
                runningNumberLength is < 3 or > 7)
            {
                throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
            }

            if (pattern.RunningNumberLength != runningNumberLength)
            {
                pattern.RunningNumberLength = runningNumberLength;
                changed = true;
            }
        }

        if (dto.IncludeYear.HasValue && pattern.IncludeYear != dto.IncludeYear.Value) { pattern.IncludeYear = dto.IncludeYear.Value; changed = true; }
        if (dto.IncludeMonth.HasValue && pattern.IncludeMonth != dto.IncludeMonth.Value) { pattern.IncludeMonth = dto.IncludeMonth.Value; changed = true; }
        if (dto.IncludeDepartment.HasValue && pattern.IncludeDepartment != dto.IncludeDepartment.Value) { pattern.IncludeDepartment = dto.IncludeDepartment.Value; changed = true; }
        if (changed)
        {
            throw new ConflictException("StaffMember-code changes require preview approval. Use the institution staffMember-code pattern endpoints before updating this aggregate.");
        }
        return false;
    }

    private static bool ApplyEmailConfiguration(
        NewInstitutionEmailConfigurationUpdateRequestDTO dto,
        InstitutionEmailConfig existing,
        out InstitutionEmailConfig update)
    {
        var smtpPassword = string.IsNullOrWhiteSpace(dto.SmtpPasswordEncrypted)
            ? existing.SmtpPasswordEncrypted
            : dto.SmtpPasswordEncrypted;
        update = new InstitutionEmailConfig
        {
            Id = existing.Id,
            SmtpHost = dto.SmtpHost is null ? existing.SmtpHost : dto.SmtpHost.Trim(),
            SmtpPort = dto.SmtpPort ?? existing.SmtpPort,
            SmtpUsername = dto.SmtpUsername is null ? existing.SmtpUsername : dto.SmtpUsername.Trim(),
            SmtpPasswordEncrypted = smtpPassword,
            FromEmail = dto.FromEmail is null ? existing.FromEmail : dto.FromEmail.Trim(),
            FromName = dto.FromName is null ? existing.FromName : dto.FromName.Trim(),
            IsActive = dto.IsActive ?? existing.IsActive
        };

        return !string.Equals(update.SmtpHost, existing.SmtpHost, StringComparison.Ordinal) ||
               update.SmtpPort != existing.SmtpPort ||
               !string.Equals(update.SmtpUsername, existing.SmtpUsername, StringComparison.Ordinal) ||
               !string.Equals(update.SmtpPasswordEncrypted, existing.SmtpPasswordEncrypted, StringComparison.Ordinal) ||
               !string.Equals(update.FromEmail, existing.FromEmail, StringComparison.Ordinal) ||
               !string.Equals(update.FromName, existing.FromName, StringComparison.Ordinal) ||
               update.IsActive != existing.IsActive;
    }

    private static bool AssignTrimmedIfProvided(string? submittedValue, string? existingValue, Action<string> assign)
    {
        if (string.IsNullOrWhiteSpace(submittedValue))
        {
            return false;
        }

        return AssignValue(submittedValue.Trim(), existingValue, assign);
    }

    private static bool AssignValue(string value, string? existingValue, Action<string> assign)
    {
        if (string.Equals(value, existingValue, StringComparison.Ordinal))
        {
            return false;
        }

        assign(value);
        return true;
    }

    private static string RequireNonEmpty(string value, string propertyName)
    {
        var trimmed = value.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        }

        return trimmed;
    }

    private HostInstitutionResponseDTO MapInstitution(Institution institution, string institutionEncryptionKey) =>
        new()
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
