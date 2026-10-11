// ============================================================================
// Author      : Deepesh Gupta
// Company     : Quecksilber Technologies
// Role        : CEO
// Purpose     : Defines client-editable values for creating a direct SubModule.
// ============================================================================

using System.ComponentModel.DataAnnotations;

namespace skolenza.application.DTOS.Module.SubModule
{
    /// <summary>
    /// Captures the values an authenticated Host user may provide
    /// when creating a direct child SubModule.
    /// </summary>
    /// <remarks>
    /// Institution ownership, actor information, audit values, identity values,
    /// and leaf-node behavior are determined by the server.
    /// Legacy context fields are retained only for backward compatibility
    /// and must not be treated as authoritative values.
    /// </remarks>
    public class CreateSubModuleRequestDTO
    {
        #region Legacy Compatibility Fields

        /// <summary>
        /// Gets or sets the legacy staffMember identifier supplied by older clients.
        /// </summary>
        /// <remarks>
        /// The authenticated Host user is the authoritative audit actor.
        /// This value must not be used by the SubModule create flow.
        /// </remarks>
        [Obsolete("The authenticated Host actor determines the module audit user.")]
        public long StaffMemberId { get; set; }

        /// <summary>
        /// Gets or sets the legacy institution identifier supplied by older clients.
        /// </summary>
        /// <remarks>
        /// Institution ownership is inherited from the selected Parent Module.
        /// This value must not be used by the SubModule create flow.
        /// </remarks>
        [Obsolete("Institution ownership is inherited from the selected Parent Module.")]
        public long InstitutionId { get; set; }

        /// <summary>
        /// Gets or sets the legacy role identifier supplied by older clients.
        /// </summary>
        /// <remarks>
        /// Authorization is determined from the authenticated Host request.
        /// </remarks>
        [Obsolete("Authorization is determined from the authenticated request context.")]
        public int RoleId { get; set; }

        /// <summary>
        /// Gets or sets the legacy leaf-node value supplied by older clients.
        /// </summary>
        /// <remarks>
        /// A SubModule is always persisted as a leaf node by the server.
        /// This value is ignored.
        /// </remarks>
        [Obsolete("SubModule hierarchy is established by the server.")]
        public bool? IsLeafNode { get; set; }

        #endregion

        #region Editable Module Fields

        /// <summary>
        /// Gets or sets the Parent/Header Module identifier
        /// under which this SubModule will be created.
        /// </summary>
        [Range(1, int.MaxValue)]
        public int ParentModuleId { get; set; }

        /// <summary>
        /// Gets or sets the unique module code within the applicable module scope.
        /// </summary>
        [MaxLength(50)]
        public string ModuleCode { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the module name.
        /// </summary>
        [MaxLength(100)]
        public string ModuleName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display name shown in the user interface.
        /// </summary>
        [MaxLength(100)]
        public string? DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the URL path used to navigate to the SubModule.
        /// </summary>
        [MaxLength(500)]
        public string? URLPath { get; set; }

        /// <summary>
        /// Gets or sets whether the SubModule should be displayed in the UI.
        /// </summary>
        public bool IsModuleDisplayInUI { get; set; }

        /// <summary>
        /// Gets or sets whether the SubModule belongs to the common menu.
        /// </summary>
        public bool IsCommonMenu { get; set; }

        /// <summary>
        /// Gets or sets the module scope selected by the authenticated Host admin.
        /// </summary>
        /// <remarks>
        /// The handler must validate this value against the supported
        /// Institution and Host module-scope constants.
        /// </remarks>
        public short ModuleScope { get; set; }

        /// <summary>
        /// Gets or sets whether the SubModule is active.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets the web icon path.
        /// </summary>
        [MaxLength(255)]
        public string? ImageIconWeb { get; set; }

        /// <summary>
        /// Gets or sets the mobile icon path.
        /// </summary>
        [MaxLength(255)]
        public string? ImageIconMobile { get; set; }

        /// <summary>
        /// Gets or sets the display priority of the SubModule.
        /// </summary>
        public int? ItemPriority { get; set; }

        /// <summary>
        /// Gets or sets an optional operational remark.
        /// </summary>
        [MaxLength(200)]
        public string? Remark { get; set; }

        #endregion
    }
}