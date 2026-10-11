// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the Host-side Institution onboarding request and initial configuration.
// ================================================================

using skolenza.application.DTOs.BaseDTO;

namespace skolenza.application.DTOs.Institution;

/// <summary>
/// Extends the established Institution registration contract with initial profile and location configuration.
/// </summary>
public sealed class NewInstitutionCreationRequestDTO : PermissionRequestDTO, INewInstitutionOnboardingConfiguration
{
    public int SubscriptionPlanId { get; set; }
    public int InstitutionTypeId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string InstitutionCode { get; set; } = string.Empty;
    public string CompanyEmailDomain { get; set; } = string.Empty;
    public int GenderId { get; set; }
    public string InstitutionEmail { get; set; } = string.Empty;
    public string? ContactPersonName { get; set; }
    public string? ContactNumber { get; set; }
    public int CountryId { get; set; }

    /// <summary>Gets or sets the initial Institution profile values.</summary>
    public NewInstitutionProfileRequestDTO Profile { get; set; } = new();

    /// <summary>Gets or sets the first Institution location created during onboarding.</summary>
    public NewCampusRequestDTO InitialLocation { get; set; } = new();

    /// <summary>Gets or sets the first staffMember-code generation pattern.</summary>
    public NewInstitutionStaffMemberCodePatternRequestDTO StaffMemberCodePattern { get; set; } = new();

}

/// <summary>Supplies extended onboarding values to the established transactional creation handler.</summary>
public interface INewInstitutionOnboardingConfiguration
{
    NewInstitutionProfileRequestDTO Profile { get; }
    NewCampusRequestDTO InitialLocation { get; }
}

/// <summary>Defines the initial Institution staffMember-code generation pattern.</summary>
public sealed class NewInstitutionStaffMemberCodePatternRequestDTO
{
    public string Prefix { get; set; } = "EMP";
    public bool IncludeYear { get; set; } = true;
    public bool IncludeMonth { get; set; } = true;
    public bool IncludeDepartment { get; set; }
    public string Separator { get; set; } = "/";
    public string RunningNumberLength { get; set; } = "4";
}

/// <summary>Defines editable values for the initial Institution profile.</summary>
public sealed class NewInstitutionProfileRequestDTO
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

/// <summary>Defines the first physical Institution location.</summary>
public sealed class NewCampusRequestDTO
{
    public string LocationCode { get; set; } = "HQ";
    public string? LocationName { get; set; }
    public short LocationType { get; set; } = 1;
    public int? StateId { get; set; }
    public int? DistrictId { get; set; }
    public int? LocalityId { get; set; }
    public string? Address { get; set; }
    public string? Landmark { get; set; }
    public string? PostalCode { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public int? GeoFenceRadiusMeters { get; set; }
    public string TimeZoneId { get; set; } = "Asia/Kolkata";
    public bool IsGeoFenceEnabled { get; set; }
    public bool IsAttendanceAllowed { get; set; } = true;
    public bool IsBiometricEnabled { get; set; }
}
