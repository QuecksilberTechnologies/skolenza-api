// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the request contract for Institution deletion.
// ================================================================

using skolenza.application.DTOs.BaseDTO;

namespace skolenza.application.DTOs.Institution;

/// <summary>
/// Represents the Host-side request to place a Institution into the existing soft-delete lifecycle.
/// </summary>
public sealed class DeleteInstitutionRequestDTO : PermissionRequestDTO
{
    #region Deletion Properties

    /// <summary>
    /// Gets or sets the encrypted Institution identifier to delete.
    /// </summary>
    public string InstitutionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional administrative reason for deletion.
    /// </summary>
    public string? Remark { get; set; }

    #endregion
}
