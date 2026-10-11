// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the request contract for deactivating a Institution with an administrative remark.
// ================================================================

using skolenza.application.DTOs.BaseDTO;

namespace skolenza.application.DTOs.Institution;

/// <summary>
/// Represents the Host-side request to deactivate a Institution.
/// </summary>
public sealed class DeactivateInstitutionRequestDTO : PermissionRequestDTO
{
    #region Deactivation Properties

    /// <summary>
    /// Gets or sets the encrypted Institution identifier to deactivate.
    /// </summary>
    public string InstitutionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the administrative reason for deactivating the Institution.
    /// </summary>
    public string Remark { get; set; } = string.Empty;

    #endregion
}
