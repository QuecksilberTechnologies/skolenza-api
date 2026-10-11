// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the Host-facing Institution response projection with a protected Institution identifier.
// ================================================================

namespace skolenza.application.DTOs.Institution;

/// <summary>
/// Represents Institution management data returned to an authorized Host user.
/// </summary>
public sealed class HostInstitutionResponseDTO
{
    /// <summary>
    /// Gets or sets the encrypted Institution identifier for Host API use.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the encrypted onboarding StaffMember identifier when the Institution has a valid onboarding credential.
    /// </summary>
    public string StaffMemberId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Institution company name.
    /// </summary>
    public string CompanyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Institution code.
    /// </summary>
    public string? InstitutionCode { get; set; }

    /// <summary>
    /// Gets or sets the Institution company email domain.
    /// </summary>
    public string CompanyEmailDomain { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Institution email address.
    /// </summary>
    public string InstitutionEmail { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Institution contact person name.
    /// </summary>
    public string? ContactPersonName { get; set; }

    /// <summary>
    /// Gets or sets the Institution contact number.
    /// </summary>
    public string? ContactNumber { get; set; }

    /// <summary>
    /// Gets or sets the Institution country identifier.
    /// </summary>
    public int CountryId { get; set; }

    /// <summary>
    /// Gets or sets whether the Institution email is verified.
    /// </summary>
    public bool IsVerified { get; set; }

    /// <summary>
    /// Gets or sets whether the Institution is active.
    /// </summary>
    public bool IsActive { get; set; }
}
