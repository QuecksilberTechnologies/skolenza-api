// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines client input for updating an asset category.
// ================================================================

namespace skolenza.application.DTOS.AssetDTO.category;

/// <summary>
/// Represents the client-editable values for an asset category update.
/// </summary>
public class UpdateCategoryReqestDTO
{
    /// <summary>Gets or sets the asset category identifier.</summary>
    public long Id { get; set; }

    /// <summary>Gets or sets the replacement category name.</summary>
    public string? CategoryName { get; set; }

    /// <summary>Gets or sets an optional replacement remark.</summary>
    public string? Remark { get; set; }

    /// <summary>Gets or sets whether category assets can have multiple users.</summary>
    public bool HasMultipleUser { get; set; }

    /// <summary>Gets or sets an optional active-state value.</summary>
    public bool? IsActive { get; set; }
}
