// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines client input for creating an asset type.
// ================================================================

using skolenza.application.DTOs.BaseDTO;

namespace skolenza.application.DTOS.AssetDTO.type;

/// <summary>
/// Represents the client-editable values required to create an asset type.
/// </summary>
public class AddTypeRequestDTO : PermissionRequestDTO
{
    /// <summary>
    /// Gets or sets the asset category associated with the type.
    /// </summary>
    public required long AssetCategoryId { get; set; }

    /// <summary>
    /// Gets or sets the asset type name.
    /// </summary>
    public required string TypeName { get; set; }

    /// <summary>
    /// Gets or sets an optional description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets whether the type is active.
    /// </summary>
    public required bool IsActive { get; set; }
}
