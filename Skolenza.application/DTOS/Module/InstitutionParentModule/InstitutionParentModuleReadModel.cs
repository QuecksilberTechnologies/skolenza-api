// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Represents raw Institution entitlement and global Module metadata returned from Institution Parent Module persistence queries.
// ================================================================

namespace skolenza.application.DTOS.Module.InstitutionParentModule;

/// <summary>
/// Represents the repository-safe Institution entitlement record used before Host-facing Institution identifier protection.
/// </summary>
public sealed class InstitutionParentModuleReadModel
{
    /// <summary>Gets or sets the numeric Institution identifier from persistence.</summary>
    public long InstitutionId { get; set; }

    /// <summary>Gets or sets the global Module identifier.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the global Module code.</summary>
    public string? ModuleCode { get; set; }

    /// <summary>Gets or sets the global Module name.</summary>
    public string? ModuleName { get; set; }

    /// <summary>Gets or sets the global Module display name.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets the global Module URL path.</summary>
    public string? UrlPath { get; set; }

    /// <summary>Gets or sets the global Module web icon.</summary>
    public string? ImageIconWeb { get; set; }

    /// <summary>Gets or sets the global Module mobile icon.</summary>
    public string? ImageIconMobile { get; set; }

    /// <summary>Gets or sets the global Module display priority.</summary>
    public int? ItemPriority { get; set; }

    /// <summary>Gets or sets the preserved Institution entitlement parent global Module identifier.</summary>
    public int? ParentModuleId { get; set; }

    /// <summary>Gets or sets whether the Institution entitlement represents a leaf Module.</summary>
    public bool? IsLeafNode { get; set; }

    /// <summary>Gets or sets whether the Institution has this Module enabled.</summary>
    public bool IsEnabled { get; set; }

    /// <summary>Gets or sets the global Module scope.</summary>
    public short ModuleScope { get; set; }

    /// <summary>Gets or sets direct entitled Header children.</summary>
    public List<InstitutionParentModuleReadModel> Children { get; set; } = new();
}
