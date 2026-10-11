// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines client-editable values for creating a institution role.
// ================================================================

using skolenza.application.DTOs.BaseDTO;

namespace skolenza.application.DTOs.Role
{
    /// <summary>
    /// Represents client-supplied values for a new institution role.
    /// </summary>
    public class CreateRoleRequestDTO : PermissionRequestDTO
    {
        public string RoleName { get; set; } = string.Empty;
        public int RoleType { get; set; }
        public string? Remark { get; set; }
        public bool IsActive { get; set; }
    }
}
