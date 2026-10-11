// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines client input for deleting an asset status.
// ================================================================

namespace skolenza.application.DTOS.AssetDTO.status;

/// <summary>
/// Represents the asset status selected for deletion.
/// </summary>
public class DeleteStatusReqestDTO
{
    /// <summary>
    /// Gets or sets the asset status identifier.
    /// </summary>
    public int Id { get; set; }
}
