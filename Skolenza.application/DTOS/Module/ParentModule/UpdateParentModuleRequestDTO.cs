// ============================================================================
// Author      : Deepesh Gupta
// Company     : Quecksilber Technologies
// Role        : CEO
// Purpose     : Defines client-editable values for updating a Parent/Header Module.
// ============================================================================

using System.ComponentModel.DataAnnotations;

namespace skolenza.application.DTOS.Module.ParentModule
{
    /// <summary>
    /// Captures the values that may change without altering Parent/Header Module ownership or hierarchy.
    /// </summary>
    public class UpdateParentModuleRequestDTO
    {
        /// <summary>Gets or sets the module code.</summary>
        [MaxLength(50)]
        public string ModuleCode { get; set; } = string.Empty;

        /// <summary>Gets or sets the module name.</summary>
        [MaxLength(100)]
        public string ModuleName { get; set; } = string.Empty;

        /// <summary>Gets or sets the UI display name.</summary>
        [MaxLength(100)]
        public string? DisplayName { get; set; }

        /// <summary>Gets or sets the module URL path.</summary>
        [MaxLength(500)]
        public string? URLPath { get; set; }

        /// <summary>Gets or sets whether the module is displayed in the UI.</summary>
        public bool IsModuleDisplayInUI { get; set; }

        /// <summary>Gets or sets whether the module represents the common menu.</summary>
        public bool IsCommonMenu { get; set; }

        /// <summary>
        /// Gets or sets the existing module scope used to locate the Header Module.
        /// </summary>
        /// <remarks>The scope is immutable during update.</remarks>
        public short ModuleScope { get; set; }

        /// <summary>Gets or sets whether the module is active.</summary>
        public bool IsActive { get; set; }

        /// <summary>Gets or sets the web icon path.</summary>
        [MaxLength(255)]
        public string? ImageIconWeb { get; set; }

        /// <summary>Gets or sets the mobile icon path.</summary>
        [MaxLength(255)]
        public string? ImageIconMobile { get; set; }

        /// <summary>Gets or sets the display order.</summary>
        public int? ItemPriority { get; set; }

        /// <summary>Gets or sets an optional operational remark.</summary>
        [MaxLength(200)]
        public string? Remark { get; set; }
    }
}
