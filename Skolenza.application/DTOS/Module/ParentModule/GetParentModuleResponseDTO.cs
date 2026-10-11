// ============================================================================
// Author      : Deepesh Gupta
// Company     : Quecksilber Technologies
// Role        : CEO
// Purpose     : Represents Parent/Header Module data returned by CRUD operations.
// ============================================================================

namespace skolenza.application.DTOS.Module.ParentModule
{
    /// <summary>
    /// Describes a Parent/Header Module after institution and scope filtering has been applied.
    /// </summary>
    public class GetParentModuleResponseDTO
    {
        /// <summary>Gets or sets the database-generated module identifier.</summary>
        public int Id { get; set; }

        /// <summary>Gets or sets the module code.</summary>
        public string? ModuleCode { get; set; }

        /// <summary>Gets or sets the module name.</summary>
        public string? ModuleName { get; set; }

        /// <summary>Gets or sets the UI display name.</summary>
        public string? DisplayName { get; set; }

        /// <summary>Gets or sets the module URL path.</summary>
        public string? URLPath { get; set; }

        /// <summary>Gets or sets the Parent Module identifier, which is null for a Header Module.</summary>
        public int? ParentModuleId { get; set; }

        /// <summary>Gets or sets whether the module is a leaf node.</summary>
        public bool? IsLeafNode { get; set; }

        /// <summary>Gets or sets whether the module is displayed in the UI.</summary>
        public bool IsModuleDisplayInUI { get; set; }

        /// <summary>Gets or sets whether the module represents the common menu.</summary>
        public bool IsCommonMenu { get; set; }

        /// <summary>Gets or sets the module scope.</summary>
        public short ModuleScope { get; set; }

        /// <summary>Gets or sets whether the module is active.</summary>
        public bool IsActive { get; set; }

        /// <summary>Gets or sets the web icon path.</summary>
        public string? ImageIconWeb { get; set; }

        /// <summary>Gets or sets the mobile icon path.</summary>
        public string? ImageIconMobile { get; set; }

        /// <summary>Gets or sets the display order.</summary>
        public int? ItemPriority { get; set; }

        /// <summary>Gets or sets the optional operational remark.</summary>
        public string? Remark { get; set; }

        /// <summary>Gets or sets the identifier of the user that created the module.</summary>
        public long? AddedById { get; set; }

        /// <summary>Gets or sets when the module was created.</summary>
        public DateTime? AddedDateTime { get; set; }

        /// <summary>Gets or sets the identifier of the user that last updated the module.</summary>
        public long? UpdatedById { get; set; }

        /// <summary>Gets or sets when the module was last updated.</summary>
        public DateTime? UpdatedDateTime { get; set; }
    }
}
