// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines client-editable values for updating a institution role.
// ================================================================

using skolenza.application.DTOs.BaseDTO;

namespace skolenza.application.DTOs.Role
{
    /// <summary>
    /// Represents client-editable values for an existing institution role.
    /// </summary>
    public class UpdateRoleRequestDTO : PermissionRequestDTO
    {
        public required int Id { get; set; }
        public string? RoleName { get; set; }
        public int RoleType { get; set; }
        public string? Remark { get; set; }
        public bool? IsActive { get; set; }
    }
}
