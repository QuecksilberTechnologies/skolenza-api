// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the lightweight, token-derived navigation contract for the application shell.
// ================================================================

namespace skolenza.application.DTOS.Navigation;

/// <summary>
/// Represents the complete navigation tree available to the current authenticated principal.
/// </summary>
public sealed class NavigationMenuResponseDTO
{
    /// <summary>Gets the authenticated principal type that owns the returned navigation tree.</summary>
    public string UserType { get; init; } = string.Empty;

    /// <summary>Gets the ordered root navigation items available to the authenticated principal.</summary>
    public IReadOnlyCollection<NavigationMenuItemResponseDTO> Items { get; init; }
        = Array.Empty<NavigationMenuItemResponseDTO>();
}

/// <summary>
/// Represents one permitted Module in the application navigation hierarchy.
/// </summary>
public sealed class NavigationMenuItemResponseDTO
{
    public int Id { get; init; }
    public string? ModuleCode { get; init; }
    public string ModuleName { get; init; } = string.Empty;
    /// <summary>
    /// Gets the immutable frontend page-component key. This is independent of
    /// the mutable <see cref="UrlPath"/> navigation route.
    /// </summary>
    public string? PageName { get; init; }
    public string? DisplayName { get; init; }
    public string? UrlPath { get; init; }
    public string? IconKey { get; init; }
    public int? ParentModuleId { get; init; }
    public bool IsLeafNode { get; init; }
    public short ModuleScope { get; init; }

    /// <summary>Gets the operations currently allowed for this Module.</summary>
    public IReadOnlyCollection<NavigationOperationResponseDTO> Operations { get; init; }
        = Array.Empty<NavigationOperationResponseDTO>();

    /// <summary>Gets the permitted child Modules.</summary>
    public IReadOnlyCollection<NavigationMenuItemResponseDTO> Children { get; init; }
        = Array.Empty<NavigationMenuItemResponseDTO>();
}

/// <summary>
/// Represents one operation already authorized for the parent navigation Module.
/// </summary>
public sealed class NavigationOperationResponseDTO
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? IconKey { get; init; }
}
