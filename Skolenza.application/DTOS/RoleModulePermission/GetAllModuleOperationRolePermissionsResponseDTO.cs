// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines role module-operation permission response models.
// ================================================================

namespace skolenza.application.DTOs.RoleModulePermission
{
    /// <summary>
    /// Represents the module-operation permissions assigned to a role.
    /// </summary>
    public class GetAllModuleOperationRolePermissionsResponseDTO
    {
        public int RoleId { get; set; }

        public List<ModulePermissionDTO> ModuleOperations { get; set; } = new();
    }

    /// <summary>
    /// Represents operation permissions selected for a module.
    /// </summary>
    public class ModulePermissionDTO
    {
        public int ModuleId { get; set; }

        public string? ModuleName { get; set; }

        public List<OperationPermissionDTO> Operations { get; set; } = new();
    }

    /// <summary>
    /// Represents a role permission for one module operation.
    /// </summary>
    public class OperationPermissionDTO
    {
        public int OperationId { get; set; }

        public string? OperationName { get; set; }

        public int OperationType { get; set; }

        public bool HasAccess { get; set; }
    }
}
