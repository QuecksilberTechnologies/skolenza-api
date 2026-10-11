// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines client input for creating an asset category.
// ================================================================

namespace skolenza.application.DTOS.AssetDTO.category;

/// <summary>
/// Represents the client-editable values required to create an asset category.
/// </summary>
public class AddCategoryReqestDTO
{
    /// <summary>Gets or sets the category name.</summary>
    public string CategoryName { get; set; } = null!;

    /// <summary>Gets or sets an optional category remark.</summary>
    public string? Remark { get; set; }

    /// <summary>Gets or sets whether the category is active.</summary>
    public bool IsActive { get; set; }

    /// <summary>Gets or sets whether category assets can have multiple users.</summary>
    public bool HasMultipleUser { get; set; }
}
