// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines client input for deleting an asset type.
// ================================================================

using skolenza.application.DTOs.BaseDTO;

namespace skolenza.application.DTOS.AssetDTO.type;

/// <summary>
/// Represents the asset type selected for deletion.
/// </summary>
public class DeleteTypeRequestDTO : PermissionRequestDTO
{
    /// <summary>
    /// Gets or sets the asset type identifier.
    /// </summary>
    public long Id { get; set; }
}
