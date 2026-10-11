// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines client input for creating an asset status.
// ================================================================

using skolenza.application.DTOs.BaseDTO;

namespace skolenza.application.DTOS.AssetDTO.status;

/// <summary>
/// Represents the client-editable values required to create an asset status.
/// </summary>
public class CreateStatusRequestDTO : PermissionRequestDTO
{
    /// <summary>
    /// Gets or sets the asset status name.
    /// </summary>
    public string StatusName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the display color key.
    /// </summary>
    public string? ColorKey { get; set; }

    /// <summary>
    /// Gets or sets an optional description.
    /// </summary>
    public string? Description { get; set; }
}
