// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines client-editable values for updating a designation.
// ================================================================

using skolenza.application.DTOs.BaseDTO;

namespace skolenza.application.DTOs.Designation
{
    /// <summary>
    /// Represents the client-editable values for an existing designation.
    /// </summary>
    public class UpdateDesignationRequestDTO : PermissionRequestDTO
    {
        public required int Id { get; set; }
        public int DepartmentId { get; set; }
        public string? DesignationName { get; set; }
        public string? Description { get; set; }
        public bool? IsActive { get; set; }
    }
}
