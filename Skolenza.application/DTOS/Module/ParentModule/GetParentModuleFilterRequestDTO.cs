// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the shared Host-admin query filters for Parent Module retrieval.
// ================================================================

namespace skolenza.application.DTOS.Module.ParentModule;

/// <summary>
/// Represents the module scope and optional active-state filter used to retrieve Parent Module headers.
/// </summary>
public sealed class GetParentModuleFilterRequestDTO
{
    /// <summary>
    /// Gets or sets the required module scope.
    /// </summary>
    public short ModuleScope { get; set; }

    /// <summary>
    /// Gets or sets the optional active-state filter.
    /// </summary>
    public bool? IsActive { get; set; }
}
