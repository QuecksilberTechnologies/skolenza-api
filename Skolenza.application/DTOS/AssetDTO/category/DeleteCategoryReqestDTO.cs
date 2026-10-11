// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines client input for deleting an asset category.
// ================================================================

namespace skolenza.application.DTOS.AssetDTO.category;

/// <summary>
/// Represents the asset category selected for deletion.
/// </summary>
public class DeleteCategoryReqestDTO
{
    /// <summary>Gets or sets the asset category identifier.</summary>
    public long Id { get; set; }
}
