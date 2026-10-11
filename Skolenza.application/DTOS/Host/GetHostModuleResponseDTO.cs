// ============================================================================
// Author      : Deepesh Gupta
// Company     : Quecksilber Technologies
// Role        : CEO
// Purpose     : Represents module details returned by Host module read operations.
// ============================================================================

namespace skolenza.application.DTOS.Host
{
    /// <summary>
    /// Represents the module fields returned by the Host module API.
    /// </summary>
    public class GetHostModuleResponseDTO
    {
        #region Properties

        /// <summary>
        /// Gets or sets the module identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the institution identifier associated with the module.
        /// </summary>
        public long? InstitutionId { get; set; }

        /// <summary>
        /// Gets or sets the module code.
        /// </summary>
        public string? ModuleCode { get; set; }

        /// <summary>
        /// Gets or sets the module name.
        /// </summary>
        public string ModuleName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the display name shown to users.
        /// </summary>
        public string? DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the module URL path.
        /// </summary>
        public string? Urlpath { get; set; }

        /// <summary>
        /// Gets or sets the parent module identifier.
        /// </summary>
        public int? ParentModuleId { get; set; }

        /// <summary>
        /// Gets or sets whether the module is a leaf node.
        /// </summary>
        public bool? IsLeafNode { get; set; }

        /// <summary>
        /// Gets or sets whether the module is displayed in the user interface.
        /// </summary>
        public bool IsModuleDisplayInUi { get; set; }

        /// <summary>
        /// Gets or sets whether the module belongs to the common menu.
        /// </summary>
        public bool IsCommonMenu { get; set; }

        /// <summary>
        /// Gets or sets the module scope.
        /// </summary>
        public short ModuleScope { get; set; }

        /// <summary>
        /// Gets or sets whether the module is active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the web icon path.
        /// </summary>
        public string? ImageIconWeb { get; set; }

        /// <summary>
        /// Gets or sets the mobile icon path.
        /// </summary>
        public string? ImageIconMobile { get; set; }

        /// <summary>
        /// Gets or sets the display priority.
        /// </summary>
        public int? ItemPriority { get; set; }

        /// <summary>
        /// Gets or sets the module remark.
        /// </summary>
        public string? Remark { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the user who added the module.
        /// </summary>
        public long? AddedById { get; set; }

        /// <summary>
        /// Gets or sets the date and time at which the module was added.
        /// </summary>
        public DateTime? AddedDateTime { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the user who last updated the module.
        /// </summary>
        public long? UpdatedById { get; set; }

        /// <summary>
        /// Gets or sets the date and time at which the module was last updated.
        /// </summary>
        public DateTime? UpdatedDateTime { get; set; }

        #endregion
    }
}
