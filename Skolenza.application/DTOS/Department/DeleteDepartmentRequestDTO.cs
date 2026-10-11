// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines client-supplied data for deleting a department.
// ================================================================

using skolenza.application.DTOs.BaseDTO;

namespace skolenza.application.DTOs.Department
{
    /// <summary>
    /// Represents the client-supplied data used to delete a department.
    /// </summary>
    public class DeleteDepartmentRequestDTO : PermissionRequestDTO
    {
        /// <summary>
        /// Gets or sets the department identifier to delete.
        /// </summary>
        public required int Id { get; set; }

        /// <summary>
        /// Gets or sets the encoded staffMember identifier supplied by the client.
        /// </summary>
        public required string UserStaffMemberId { get; set; }
    }
}

