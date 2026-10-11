// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines editable Institution fields for Host-side Institution management.
// ================================================================

namespace skolenza.application.DTOs.Institution;

/// <summary>
/// Represents the client-editable Institution fields for a Host-side update request.
/// </summary>
public sealed class UpdateInstitutionRequestDTO : UpdateHostManagedInstitutionRequestDTO
{
    #region Institution Identifier

    /// <summary>
    /// Gets or sets the encrypted Institution identifier to update.
    /// </summary>
    public string InstitutionId { get; set; } = string.Empty;

    #endregion
}
