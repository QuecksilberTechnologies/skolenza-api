// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the request model for assigning module operations to a role.
// ================================================================

using skolenza.application.DTOs.BaseDTO;

namespace skolenza.application.DTOs.RoleModulePermission
{
    /// <summary>
    /// Defines client-editable role permission assignments.
    /// </summary>
    public class CreateModuleOperationRolePermissionsRequestDTO : PermissionRequestDTO
    {
        public int RoleId { get; set; }

        public List<ModulePermissionDTO>? ModuleOperations { get; set; }
    }
}
