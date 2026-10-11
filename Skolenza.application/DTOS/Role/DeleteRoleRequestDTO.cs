// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines a request to soft delete a institution role.
// ================================================================

using skolenza.application.DTOs.BaseDTO;

namespace skolenza.application.DTOs.Role
{
    /// <summary>
    /// Represents the identifier of a institution role to soft delete.
    /// </summary>
    public class DeleteRoleRequestDTO : PermissionRequestDTO
    {
        public required int Id { get; set; }
    }
}
