// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the active-state change requested for a Parent Module status cascade.
// ================================================================

namespace skolenza.application.DTOS.Module.ParentModule
{
    /// <summary>
    /// Captures the requested active state for a Parent Module and its status cascade.
    /// </summary>
    public class UpdateParentModuleStatusRequestDTO
    {
        /// <summary>
        /// Gets or sets the module scope used to locate the Header Module.
        /// </summary>
        public short ModuleScope { get; set; }

        /// <summary>
        /// Gets or sets the target active state.
        /// </summary>
        public bool IsActive { get; set; }
    }
}
