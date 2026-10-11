// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines client-editable values for creating a ModuleOperation mapping.
// ================================================================

using System.ComponentModel.DataAnnotations;

namespace skolenza.application.DTOs.ModuleOperation;

/// <summary>
/// Represents the client-editable values required to create one module-operation mapping.
/// </summary>
public class CreateModuleOperationRequestDTO
{
    /// <summary>Gets or sets the related module identifier.</summary>
    [Required]
    public int ModuleId { get; set; }

    /// <summary>Gets or sets the related operation identifier.</summary>
    [Required]
    public int OperationId { get; set; }

    /// <summary>Gets or sets the optional data-view structure identifier.</summary>
    public int? DataViewStructureId { get; set; }

    /// <summary>Gets or sets the optional page-type identifier.</summary>
    public int? PageTypeId { get; set; }

    /// <summary>Gets or sets the optional page URL.</summary>
    [MaxLength(255)]
    public string? PageURL { get; set; }

    /// <summary>Gets or sets the optional icon URL.</summary>
    [MaxLength(255)]
    public string? IconURL { get; set; }

    /// <summary>Gets or sets whether the mapping is a common item.</summary>
    public bool? IsCommonItem { get; set; }

    /// <summary>Gets or sets whether the mapping is operational.</summary>
    public bool? IsOperational { get; set; }

    /// <summary>Gets or sets the optional display priority.</summary>
    public int? Priority { get; set; }

    /// <summary>Gets or sets the optional remark.</summary>
    [MaxLength(255)]
    public string? Remark { get; set; }

    /// <summary>Gets or sets whether the mapping is active.</summary>
    public bool? IsActive { get; set; }
}
