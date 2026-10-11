// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the request contract for retrieving one Institution by identifier.
// ================================================================

using skolenza.application.DTOs.BaseDTO;

namespace skolenza.application.DTOs.Institution;

/// <summary>
/// Represents the request to retrieve one Institution for Host-side details or editing.
/// </summary>
public sealed class GetInstitutionByIdRequestDTO : PermissionRequestDTO
{
    #region Identifier

    /// <summary>
    /// Gets or sets the encrypted Institution identifier to retrieve.
    /// </summary>
    public string InstitutionId { get; set; } = string.Empty;

    #endregion
}
