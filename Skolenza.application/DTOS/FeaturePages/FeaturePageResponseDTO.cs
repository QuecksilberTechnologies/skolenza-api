// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the public feature-page metadata returned from active master modules and operations.
// ================================================================

using System.Text.Json.Serialization;

namespace skolenza.application.DTOS.FeaturePages;

/// <summary>
/// Represents one active master Module returned as either a feature header or an operational leaf page.
/// </summary>
public sealed class FeaturePageResponseDTO
{
    /// <summary>
    /// Gets or sets the master Module identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the optional master Module code.
    /// </summary>
    public string? ModuleCode { get; set; }

    /// <summary>
    /// Gets or sets the master Module name.
    /// </summary>
    public string ModuleName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UI display name.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the Angular route configured for the Module.
    /// </summary>
    public string? UrlPath { get; set; }

    /// <summary>
    /// Gets or sets the web icon key configured for the Module.
    /// </summary>
    public string? IconKey { get; set; }

    /// <summary>
    /// Gets or sets the direct parent Module identifier, when configured.
    /// </summary>
    public int? ParentModuleId { get; set; }

    /// <summary>
    /// Gets or sets whether this Module is an operational leaf page.
    /// </summary>
    public bool IsLeafNode { get; set; }

    /// <summary>
    /// Gets or sets whether this Module is configured to display in the UI.
    /// </summary>
    public bool IsModuleDisplayInUI { get; set; }

    /// <summary>
    /// Gets or sets the feature-page scope: 1 for Institution, 2 for Host, or 3 for Common.
    /// </summary>
    public short ModuleScope { get; set; }

    /// <summary>
    /// Gets or sets the readable feature-page scope name.
    /// </summary>
    public string ModuleScopeName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the source Module is configured as a Common-menu root.
    /// This field is used only while assembling the response hierarchy.
    /// </summary>
    [JsonIgnore]
    public bool IsCommonMenu { get; set; }

    /// <summary>
    /// Gets or sets the flat child headers below this feature header.
    /// Present only on top-level feature headers.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<FeaturePageResponseDTO>? ChildHeaders { get; set; }

    /// <summary>
    /// Gets or sets the flat operational leaf pages below this header.
    /// Present only on a header Module.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<FeaturePageResponseDTO>? OperationalPages { get; set; }

    /// <summary>
    /// Gets or sets the active operation mappings for this operational leaf page.
    /// Present only when <see cref="IsLeafNode"/> is <see langword="true"/>.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<FeaturePageOperationResponseDTO>? Operations { get; set; }
}

/// <summary>
/// Represents one active Operation master and its active Module-operation page configuration.
/// </summary>
public sealed class FeaturePageOperationResponseDTO
{
    /// <summary>
    /// Gets or sets the Operation master identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the Module-operation mapping identifier.
    /// </summary>
    public int ModuleOperationMappingId { get; set; }

    /// <summary>
    /// Gets or sets the Operation master name.
    /// </summary>
    public string OperationName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Operation type.
    /// </summary>
    public int? OperationType { get; set; }

    /// <summary>
    /// Gets or sets the Operation master remark.
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// Gets or sets the operation page icon key. Mapping-level configuration takes precedence over the Operation master icon.
    /// </summary>
    public string? IconKey { get; set; }

    /// <summary>
    /// Gets or sets the Angular page route configured for this operation on its Module.
    /// </summary>
    public string? PageUrl { get; set; }

    /// <summary>
    /// Gets or sets the optional data-view structure identifier.
    /// </summary>
    public int? DataViewStructureId { get; set; }

    /// <summary>
    /// Gets or sets the optional page-type identifier.
    /// </summary>
    public int? PageTypeId { get; set; }

    /// <summary>
    /// Gets or sets whether this operation is configured as a common item.
    /// </summary>
    public bool? IsCommonItem { get; set; }

    /// <summary>
    /// Gets or sets whether this operation is operational.
    /// </summary>
    public bool? IsOperational { get; set; }

    /// <summary>
    /// Gets or sets the configured operation display priority.
    /// </summary>
    public int? Priority { get; set; }
}
