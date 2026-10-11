// ============================================================================
// Author      : Deepesh Gupta
// Company     : Quecksilber Technologies
// Role        : CEO
// Purpose     : Defines client-editable values for creating a Parent/Header Module.
// ============================================================================

using System;
using System.ComponentModel.DataAnnotations;

namespace skolenza.application.DTOS.Module.ParentModule
{
    /// <summary>
    /// Captures the values a client may provide to create a Parent/Header Module.
    /// </summary>
    /// <remarks>
    /// Institution, actor, audit, identity, and hierarchy values are established by the server.
    /// Legacy context fields remain only to avoid breaking existing request contracts.
    /// </remarks>
    public class CreateParentModuleRequestDTO
    {
        #region Legacy Compatibility Fields

        /// <summary>
        /// Gets or sets the legacy staffMember identifier supplied by older clients.
        /// </summary>
        /// <remarks>The authenticated actor is authoritative; this value is ignored.</remarks>
        [Obsolete("The authenticated actor determines the module audit user.")]
        public long StaffMemberId { get; set; }

        /// <summary>
        /// Gets or sets the target institution identifier when a Host creates a Institution-scope master module.
        /// </summary>
        /// <remarks>A Host-scope module always ignores this value and is persisted without a institution.</remarks>
        public long InstitutionId { get; set; }

        /// <summary>
        /// Gets or sets the legacy role identifier supplied by older clients.
        /// </summary>
        /// <remarks>Parent Module CRUD does not use a client-supplied role identifier.</remarks>
        [Obsolete("Authorization is determined from the authenticated request context.")]
        public long RoleId { get; set; }

        #endregion

        #region Editable Module Fields

        /// <summary>
        /// Gets or sets the unique code for the module within the authenticated institution and scope.
        /// </summary>
        [MaxLength(50)]
        public string ModuleCode { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the module name.
        /// </summary>
        [MaxLength(100)]
        public string ModuleName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the UI display name.
        /// </summary>
        [MaxLength(100)]
        public string? DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the URL path used to open the module.
        /// </summary>
        [MaxLength(500)]
        public string? URLPath { get; set; }

        /// <summary>
        /// Gets or sets whether the module is shown in the UI.
        /// </summary>
        public bool IsModuleDisplayInUI { get; set; }

        /// <summary>
        /// Gets or sets whether the Header Module represents the common menu.
        /// </summary>
        public bool IsCommonMenu { get; set; }

        /// <summary>
        /// Gets or sets the requested module scope, which is validated for an authenticated Host user.
        /// </summary>
        public short ModuleScope { get; set; }

        /// <summary>
        /// Gets or sets whether the new module is active.
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
        /// Gets or sets the display order.
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
