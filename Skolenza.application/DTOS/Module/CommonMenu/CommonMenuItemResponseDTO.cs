// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the hierarchical Common navigation contract returned to authenticated users.
// ================================================================

namespace skolenza.application.DTOS.Module.CommonMenu;

/// <summary>
/// Represents one active, UI-visible item in the shared Common navigation hierarchy.
/// </summary>
public sealed class CommonMenuItemResponseDTO
{
    #region Properties

    /// <summary>
    /// Gets or sets the module identifier.
    /// </summary>
    public int ModuleId { get; init; }

    /// <summary>
    /// Gets or sets the module name.
    /// </summary>
    public string ModuleName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the user-facing display name when it is configured.
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// Gets or sets the configured navigation URL when it is available.
    /// </summary>
    public string? UrlPath { get; init; }

    /// <summary>
    /// Gets or sets the web icon path when it is configured.
    /// </summary>
    public string? ImageIconWeb { get; init; }

    /// <summary>
    /// Gets or sets the mobile icon path when it is configured.
    /// </summary>
    public string? ImageIconMobile { get; init; }

    /// <summary>
    /// Gets or sets whether the item is a leaf node.
    /// </summary>
    public bool IsLeafNode { get; init; }

    /// <summary>
    /// Gets or sets the configured menu priority when one exists.
    /// </summary>
    public int? ItemPriority { get; init; }

    /// <summary>
    /// Gets or sets the ordered child navigation items. Leaf nodes always contain an empty collection.
    /// </summary>
    public IReadOnlyCollection<CommonMenuItemResponseDTO> Children { get; init; }
        = Array.Empty<CommonMenuItemResponseDTO>();

    #endregion
}
