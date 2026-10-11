// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the response returned for a ModuleOperation mapping.
// ================================================================

namespace skolenza.application.DTOs.Module;

/// <summary>
/// Represents a module-operation mapping with related display values.
/// </summary>
public class ModuleOperationMappingByProductOwnerResponseDTO
{
    /// <summary>Gets or sets the mapping identifier.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the related module identifier.</summary>
    public int ModuleId { get; set; }

    /// <summary>Gets or sets the related operation identifier.</summary>
    public int OperationId { get; set; }

    /// <summary>Gets or sets the optional data-view structure identifier.</summary>
    public int? DataViewStructureId { get; set; }

    /// <summary>Gets or sets the optional page-type identifier.</summary>
    public int? PageTypeId { get; set; }

    /// <summary>Gets or sets the related module name.</summary>
    public string? ModuleName { get; set; }

    /// <summary>Gets or sets the related operation name.</summary>
    public string? OperationName { get; set; }

    /// <summary>Gets or sets the data-view display value.</summary>
    public string? DataViewStructureDisplayOn { get; set; }

    /// <summary>Gets or sets the page-type name.</summary>
    public string? PageTypeName { get; set; }

    /// <summary>Gets or sets the optional page URL.</summary>
    public string? PageURL { get; set; }

    /// <summary>Gets or sets the optional icon URL.</summary>
    public string? IconURL { get; set; }

    /// <summary>Gets or sets whether the mapping is a common item.</summary>
    public bool? IsCommonItem { get; set; }

    /// <summary>Gets or sets whether the mapping is operational.</summary>
    public bool? IsOperational { get; set; }

    /// <summary>Gets or sets the optional display priority.</summary>
    public int? Priority { get; set; }

    /// <summary>Gets or sets the optional remark.</summary>
    public string? Remark { get; set; }

    /// <summary>Gets or sets whether the mapping is active.</summary>
    public bool? IsActive { get; set; }

    /// <summary>Gets or sets the creating Host user identifier.</summary>
    public long AddedById { get; set; }

    /// <summary>Gets or sets the creation time.</summary>
    public DateTime AddedDateTime { get; set; }

    /// <summary>Gets or sets the last updating Host user identifier.</summary>
    public long? UpdatedById { get; set; }

    /// <summary>Gets or sets the last update time.</summary>
    public DateTime? UpdatedDateTime { get; set; }
}
