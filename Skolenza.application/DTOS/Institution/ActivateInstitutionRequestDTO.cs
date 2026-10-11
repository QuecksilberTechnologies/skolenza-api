// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the request contract for activating a Institution with an administrative remark.
// ================================================================

using skolenza.application.DTOs.BaseDTO;

namespace skolenza.application.DTOs.Institution;

/// <summary>
/// Represents the Host-side request to activate a Institution.
/// </summary>
public sealed class ActivateInstitutionRequestDTO : PermissionRequestDTO
{
    #region Activation Properties

    /// <summary>
    /// Gets or sets the encrypted Institution identifier to activate.
    /// </summary>
    public string InstitutionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the administrative reason for activating the Institution.
    /// </summary>
    public string Remark { get; set; } = string.Empty;

    #endregion
}
