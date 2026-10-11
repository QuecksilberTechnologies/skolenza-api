// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines client input for updating an asset type.
// ================================================================

using skolenza.application.DTOs.BaseDTO;

namespace skolenza.application.DTOS.AssetDTO.type;

/// <summary>
/// Represents the client-editable values for an asset type update.
/// </summary>
public class UpdateTypeRequestDTO : PermissionRequestDTO
{
    /// <summary>
    /// Gets or sets the asset type identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets an optional replacement category identifier.
    /// </summary>
    public long? CategoryId { get; set; }

    /// <summary>
    /// Gets or sets an optional replacement type name.
    /// </summary>
    public string? TypeName { get; set; }

    /// <summary>
    /// Gets or sets an optional replacement description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets an optional active-state value.
    /// </summary>
    public bool? IsActive { get; set; }
}
