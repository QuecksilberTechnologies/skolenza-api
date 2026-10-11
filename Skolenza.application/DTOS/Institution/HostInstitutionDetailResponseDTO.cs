// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the safe Host-side Institution detail projection, including active Institution-owned configuration collections.
// ================================================================

namespace skolenza.application.DTOs.Institution;

/// <summary>
/// Represents the safe, complete Host-visible Institution configuration detail. The Institution identifier is encrypted and no child DTO exposes a raw InstitutionId.
/// </summary>
public sealed class HostInstitutionDetailResponseDTO
{
    #region Institution

    /// <summary>Gets or sets the encrypted Institution identifier.</summary>
    public string Id { get; set; } = string.Empty;
    public int InstitutionTypeId { get; set; }
    public string? InstitutionTypeName { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? InstitutionCode { get; set; }
    public string CompanyEmailDomain { get; set; } = string.Empty;
    public string InstitutionEmail { get; set; } = string.Empty;
    public string? ContactPersonName { get; set; }
    public int? GenderId { get; set; }
    public string? ContactNumber { get; set; }
    public int CountryId { get; set; }
    public int? DefaultCurrency { get; set; }
    public bool IsVerified { get; set; }
    public bool IsActive { get; set; }

    #endregion

    #region Institution-Created Records

    public List<HostInstitutionProfileDetailDTO> Profiles { get; set; } = new();
    public List<HostCampusDetailDTO> Campuses { get; set; } = new();
    public List<HostInstitutionSubscriptionDetailDTO> Subscriptions { get; set; } = new();
    public List<HostInstitutionDepartmentDetailDTO> Departments { get; set; } = new();
    public List<HostInstitutionStaffMemberCodePatternDetailDTO> StaffMemberCodePatterns { get; set; } = new();
    public List<HostInstitutionStaffMemberDetailDTO> StaffMembers { get; set; } = new();
    public List<HostInstitutionLoginCredentialDetailDTO> LoginCredentials { get; set; } = new();
    public List<HostInstitutionEmailConfigurationDetailDTO> EmailConfigurations { get; set; } = new();

    #endregion
}

/// <summary>Represents a Institution profile record.</summary>
public sealed class HostInstitutionProfileDetailDTO
{
    public long Id { get; set; }
    public string? LogoUrl { get; set; }
    public string? ThemeColor { get; set; }
    public string? BusinessType { get; set; }
    public string? Industry { get; set; }
    public int? TotalStaffMembers { get; set; }
    public int? TotalCampuses { get; set; }
    public int? FoundedYear { get; set; }
    public string? WebsiteUrl { get; set; }
}

/// <summary>Represents an active, non-soft-deleted Institution location.</summary>
public sealed class HostCampusDetailDTO
{
    public long Id { get; set; }
    public string LocationCode { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public short LocationType { get; set; }
    public int CountryId { get; set; }
    public int? StateId { get; set; }
    public int? DistrictId { get; set; }
    public int? LocalityId { get; set; }
    public string? Address { get; set; }
    public string? Landmark { get; set; }
    public string? PostalCode { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public int? GeoFenceRadiusMeters { get; set; }
    public string TimeZoneId { get; set; } = string.Empty;
    public bool IsHeadOffice { get; set; }
    public bool IsGeoFenceEnabled { get; set; }
    public bool IsAttendanceAllowed { get; set; }
    public bool IsBiometricEnabled { get; set; }
}

/// <summary>Represents an active Institution subscription and the selected plan summary.</summary>
public sealed class HostInstitutionSubscriptionDetailDTO
{
    public long Id { get; set; }
    public int SubscriptionPlanId { get; set; }
    public string? SubscriptionPlanName { get; set; }
    public DateTime SubscriptionStartDate { get; set; }
    public DateTime SubscriptionEndDate { get; set; }
    public bool IsTrial { get; set; }
    public string? PaymentTxnId { get; set; }
    public string? PaymentMode { get; set; }
}

/// <summary>Represents an active, non-soft-deleted Institution department.</summary>
public sealed class HostInstitutionDepartmentDetailDTO
{
    public int Id { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Remark { get; set; }
    public bool IsExecutiveOffice { get; set; }
}

/// <summary>Represents an active Institution staffMember-code pattern.</summary>
public sealed class HostInstitutionStaffMemberCodePatternDetailDTO
{
    public long Id { get; set; }
    public string? Prefix { get; set; }
    public bool IncludeYear { get; set; }
    public bool IncludeMonth { get; set; }
    public bool IncludeDepartment { get; set; }
    public string Separator { get; set; } = string.Empty;
    public int RunningNumberLength { get; set; }
    public int LastUsedNumber { get; set; }
}

/// <summary>Represents an active, non-soft-deleted Institution staffMember without credentials or security data.</summary>
public sealed class HostInstitutionStaffMemberDetailDTO
{
    public long Id { get; set; }
    public string? EmployementCode { get; set; }
    public string? FirstName { get; set; }
    public string? MiddleName { get; set; }
    public string? LastName { get; set; }
    public int? GenderId { get; set; }
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public int? StaffEmploymentTypeId { get; set; }
    public string? OfficialEmail { get; set; }
    public DateTime? DateOfOnBoarding { get; set; }
    public int CountryId { get; set; }
}

/// <summary>Represents a safe active login record without its password, tokens, device data, or network data.</summary>
public sealed class HostInstitutionLoginCredentialDetailDTO
{
    public long Id { get; set; }
    public long StaffMemberId { get; set; }
    public string LoginId { get; set; } = string.Empty;
    public bool HasFirstLogin { get; set; }
    public bool? IsPasswordChangeRequired { get; set; }
    public bool IsOnboard { get; set; }
    public string? Remark { get; set; }
}

/// <summary>Represents an active Institution SMTP configuration without password or secret-key material.</summary>
public sealed class HostInstitutionEmailConfigurationDetailDTO
{
    public int Id { get; set; }
    public string? SmtpHost { get; set; }
    public int? SmtpPort { get; set; }
    public string? SmtpUsername { get; set; }
    public string? FromEmail { get; set; }
    public string? FromName { get; set; }
}
