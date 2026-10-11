// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines a client request to soft delete a designation.
// ================================================================

using skolenza.application.DTOs.BaseDTO;

namespace skolenza.application.DTOs.Designation
{
    /// <summary>
    /// Represents the identifier of a designation to soft delete.
    /// </summary>
    public class DeleteDesignationRequestDTO : PermissionRequestDTO
    {
        public required int Id { get; set; }
    }
}
