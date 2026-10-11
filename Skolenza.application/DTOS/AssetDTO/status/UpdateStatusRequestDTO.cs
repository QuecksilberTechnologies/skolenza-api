// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines client input for updating an asset status.
// ================================================================

using skolenza.application.DTOs.BaseDTO;

namespace skolenza.application.DTOS.AssetDTO.status;

/// <summary>
/// Represents the client-editable values for an asset status update.
/// </summary>
public class UpdateStatusRequestDTO : PermissionRequestDTO
{
    /// <summary>
    /// Gets or sets the asset status identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets an optional replacement status name.
    /// </summary>
    public string? StatusName { get; set; }

    /// <summary>
    /// Gets or sets an optional replacement display color key.
    /// </summary>
    public string? ColorKey { get; set; }

    /// <summary>
    /// Gets or sets an optional replacement description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets an optional active-state value.
    /// </summary>
    public bool? IsActive { get; set; }
}
