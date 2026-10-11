// ============================================================================
// Author      : Deepesh Gupta
// Company     : Quecksilber Technologies
// Role        : CEO
// Purpose     : Defines the active-state change requested for a direct SubModule.
// ============================================================================

namespace skolenza.application.DTOS.Module.SubModule
{
    /// <summary>
    /// Captures the supported non-destructive status change for a direct child module.
    /// </summary>
    public class UpdateSubModuleStatusRequestDTO
    {
        /// <summary>Gets or sets the module scope used to locate the SubModule.</summary>
        public short ModuleScope { get; set; }

        /// <summary>Gets or sets the target active state.</summary>
        public bool IsActive { get; set; }
    }
}
