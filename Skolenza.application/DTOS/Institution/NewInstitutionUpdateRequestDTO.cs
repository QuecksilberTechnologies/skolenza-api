// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the Host-side request for the transactional nested Institution update endpoint.
// ================================================================

using skolenza.domain.Entity;

namespace skolenza.application.DTOs.Institution;

/// <summary>
/// Defines the editable Institution aggregate values accepted by the Host-managed nested update endpoint.
/// Permission metadata is intentionally supplied through the query string, not this body.
/// </summary>
public sealed class NewInstitutionUpdateRequestDTO
{
    #region Institution

    /// <summary>Gets or sets the Institution type identifier when it is being changed.</summary>
    public int InstitutionTypeId { get; set; }

    /// <summary>Gets or sets the Institution company name when it is being changed.</summary>
    public string? CompanyName { get; set; }

    /// <summary>Gets or sets the Institution code when it is being changed.</summary>
    public string? InstitutionCode { get; set; }

    /// <summary>Gets or sets the Institution company email domain when it is being changed.</summary>
    public string? CompanyEmailDomain { get; set; }

    /// <summary>Gets or sets the Institution email address when it is being changed.</summary>
    public string? InstitutionEmail { get; set; }

    /// <summary>Gets or sets the Institution contact-person name when it is being changed.</summary>
    public string? ContactPersonName { get; set; }

    /// <summary>Gets or sets the Institution contact-person gender identifier when it is being changed.</summary>
    public int? GenderId { get; set; }

    /// <summary>Gets or sets the Institution contact number when it is being changed.</summary>
    public string? ContactNumber { get; set; }

    /// <summary>Gets or sets the Institution country identifier when it is being changed.</summary>
    public int CountryId { get; set; }

    /// <summary>Gets or sets the Institution default currency identifier when it is being changed.</summary>
    public int? DefaultCurrency { get; set; }

    #endregion

    #region Nested Configuration

    /// <summary>Gets or sets the Institution profile values to update.</summary>
    public NewInstitutionProfileUpdateRequestDTO? Profile { get; set; }

    /// <summary>Gets or sets the one Institution location selected by the UI dropdown.</summary>
    public NewInstitutionSelectedLocationUpdateRequestDTO? SelectedLocation { get; set; }

    /// <summary>Gets or sets the active staffMember-code-pattern values to update.</summary>
    public NewInstitutionStaffMemberCodePatternUpdateRequestDTO? StaffMemberCodePattern { get; set; }

    /// <summary>Gets or sets the active Institution email configuration values to update.</summary>
    public NewInstitutionEmailConfigurationUpdateRequestDTO? EmailConfiguration { get; set; }

    #endregion
}

/// <summary>Defines editable Institution profile values.</summary>
public sealed class NewInstitutionProfileUpdateRequestDTO
{
    public string? LogoUrl { get; set; }
    public string? ThemeColor { get; set; }
    public string? BusinessType { get; set; }
    public string? Industry { get; set; }
    public int? TotalStaffMembers { get; set; }
    public int? TotalCampuses { get; set; }
    public int? FoundedYear { get; set; }
    public string? WebsiteUrl { get; set; }
}

/// <summary>Defines editable values for exactly one UI-selected Institution location.</summary>
public sealed class NewInstitutionSelectedLocationUpdateRequestDTO
{
    /// <summary>Gets or sets the raw identifier selected from the Institution's own location dropdown.</summary>
    public long CampusId { get; set; }

    public string? LocationCode { get; set; }
    public string? LocationName { get; set; }
    public CampusType? LocationType { get; set; }
    public int? CountryId { get; set; }
    public int? StateId { get; set; }
    public int? DistrictId { get; set; }
    public int? LocalityId { get; set; }
    public string? Address { get; set; }
    public string? Landmark { get; set; }
    public string? PostalCode { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public int? GeoFenceRadiusMeters { get; set; }
    public string? TimeZoneId { get; set; }
    public bool? IsGeoFenceEnabled { get; set; }
    public bool? IsAttendanceAllowed { get; set; }
    public bool? IsBiometricEnabled { get; set; }
}

/// <summary>Defines editable values for the active Institution staffMember-code pattern.</summary>
public sealed class NewInstitutionStaffMemberCodePatternUpdateRequestDTO
{
    public string? Prefix { get; set; }
    public bool? IncludeYear { get; set; }
    public bool? IncludeMonth { get; set; }
    public bool? IncludeDepartment { get; set; }
    public string? Separator { get; set; }
    public string? RunningNumberLength { get; set; }
}

/// <summary>Defines editable non-secret SMTP configuration values.</summary>
public sealed class NewInstitutionEmailConfigurationUpdateRequestDTO
{
    public string? SmtpHost { get; set; }
    public int? SmtpPort { get; set; }
    public string? SmtpUsername { get; set; }

    /// <summary>Gets or sets a replacement encrypted SMTP password; null, empty, and whitespace preserve the current password.</summary>
    public string? SmtpPasswordEncrypted { get; set; }

    public string? FromEmail { get; set; }
    public string? FromName { get; set; }
    public bool? IsActive { get; set; }
}
