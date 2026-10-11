// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines client-editable fields for updating a department.
// ================================================================

using skolenza.application.DTOs.BaseDTO;

namespace skolenza.application.DTOs.Department
{
    /// <summary>
    /// Represents the client-editable values for an existing department.
    /// </summary>
    public class UpdateDepartmentRequestDTO : PermissionRequestDTO
    {
        /// <summary>
        /// Gets or sets the department identifier to update.
        /// </summary>
        public required int Id { get; set; }

        /// <summary>
        /// Gets or sets the department name.
        /// </summary>
        public string? DepartmentName { get; set; }

        /// <summary>
        /// Gets or sets the department description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the department active status.
        /// </summary>
        public bool? IsActive { get; set; }

        /// <summary>
        /// Gets or sets an optional department remark.
        /// </summary>
        public string? Remark { get; set; }
    }
}
