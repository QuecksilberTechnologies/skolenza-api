// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines client-editable values for creating a designation.
// ================================================================

using skolenza.application.DTOs.BaseDTO;

namespace skolenza.application.DTOs.Designation
{
    /// <summary>
    /// Represents the client-supplied values required to create a designation.
    /// </summary>
    public class CreateDesignationRequestDTO : PermissionRequestDTO
    {
        public int DepartmentId { get; set; }
        public string? DesignationName { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
