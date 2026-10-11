// ============================================================================
// Author      : Deepesh Gupta
// Company     : Quecksilber Technologies
// Role        : CEO
// Purpose     : Represents SubModule data returned by CRUD operations.
// ============================================================================

namespace skolenza.application.DTOS.Module.SubModule
{
    /// <summary>
    /// Describes a direct child module together with a compact summary of its Header Module.
    /// </summary>
    public class GetSubModuleResponseDTO
    {
        /// <summary>Gets or sets the database-generated SubModule identifier.</summary>
        public int Id { get; set; }

        /// <summary>Gets or sets the module code.</summary>
        public string? ModuleCode { get; set; }

        /// <summary>Gets or sets the module name.</summary>
        public string? ModuleName { get; set; }

        /// <summary>Gets or sets the UI display name.</summary>
        public string? DisplayName { get; set; }

        /// <summary>Gets or sets the module URL path.</summary>
        public string? URLPath { get; set; }

        /// <summary>Gets or sets the direct Header Module identifier.</summary>
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

        /// <summary>Gets or sets an optional operational remark.</summary>
        public string? Remark { get; set; }

        /// <summary>Gets or sets the identifier of the Host user that created the module.</summary>
        public long? AddedById { get; set; }

        /// <summary>Gets or sets when the module was created.</summary>
        public DateTime? AddedDateTime { get; set; }

        /// <summary>Gets or sets the identifier of the Host user that last updated the module.</summary>
        public long? UpdatedById { get; set; }

        /// <summary>Gets or sets when the module was last updated.</summary>
        public DateTime? UpdatedDateTime { get; set; }

        /// <summary>Gets or sets the non-circular Header Module summary.</summary>
        public ParentModuleSummaryDTO? ParentModule { get; set; }
    }

    /// <summary>
    /// Describes the Header Module associated with a direct child module.
    /// </summary>
    public class ParentModuleSummaryDTO
    {
        /// <summary>Gets or sets the Header Module identifier.</summary>
        public int Id { get; set; }

        /// <summary>Gets or sets the Header Module code.</summary>
        public string? ModuleCode { get; set; }

        /// <summary>Gets or sets the Header Module name.</summary>
        public string? ModuleName { get; set; }

        /// <summary>Gets or sets the Header Module display name.</summary>
        public string? DisplayName { get; set; }

        /// <summary>Gets or sets the Header Module scope.</summary>
        public short ModuleScope { get; set; }

        /// <summary>Gets or sets whether the Header Module is active.</summary>
        public bool IsActive { get; set; }
    }
}
