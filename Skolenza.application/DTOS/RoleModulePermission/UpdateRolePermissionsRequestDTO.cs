using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.RoleModulePermission
{
    // Request DTO for save
    public class UpdateRolePermissionsRequestDTO
    {
        public long InstitutionId { get; set; }
        public int RoleId { get; set; }
        public int UpdatedById { get; set; }
        public List<ModuleOperationPermissionItem> Permissions { get; set; } = new();
    }
    public class ModuleOperationPermissionItem
    {
        public int ModuleId { get; set; }
        public int OperationId { get; set; }
        public bool HasAccess { get; set; }
    }
}
