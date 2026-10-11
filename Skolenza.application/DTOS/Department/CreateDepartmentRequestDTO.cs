using skolenza.application.DTOs.BaseDTO;

namespace skolenza.application.DTOs.Department
{
    /// <summary>
    /// Represents client-supplied data used to create a department.
    /// </summary>
    public class CreateDepartmentRequestDTO : PermissionRequestDTO
    {
        /// <summary>
        /// Gets or sets the encoded identifier of the staffMember creating the department.
        /// </summary>
        public required string UserStaffMemberId { get; set; }

        /// <summary>
        /// Gets or sets the department name.
        /// </summary>
        public string DepartmentName { get; set; } = null!;

        /// <summary>
        /// Gets or sets an optional department description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the requested active status.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets an optional remark for the department.
        /// </summary>
        public string? Remark { get; set; }
    }
}
