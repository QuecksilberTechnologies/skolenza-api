using skolenza.application.DTOS.Pagination;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.RoleModulePermission
{
  


    [Keyless]
    public class FlatModuleOperationDto
    {
        public int ModuleId { get; set; }
        public string ModuleName { get; set; } = string.Empty;
        public bool IsLeafNode { get; set; }
        public bool? IsEnabled { get; set; }
        public int Level { get; set; }
        public string BreadCrum { get; set; } = string.Empty;
        public int? ParentModuleId { get; set; }

        // ✅ Allow nulls in these properties
        public int? OperationId { get; set; }
        public string? OperationName { get; set; }   // <-- make nullable
        public bool? IsOperationUsed { get; set; }
    }


    [Keyless]
    public class GetModuleOperationRolePermissionsResponseDTO : BaseRequest
    {
        public int ModuleId { get; set; }
        public string? ModuleName { get; set; }
        public bool? IsLeafNode { get; set; }
        public bool? IsEnabled { get; set; }
        public int Level { get; set; }
        public string? BreadCrum { get; set; }

        public List<OperationDTO_Config> Operations { get; set; } = new();  // Always initialize list
        public List<GetModuleOperationRolePermissionsResponseDTO> Children { get; set; } = new(); // Same here
    }

    public class OperationDTO_Config
    {
        public int OperationId { get; set; }
        public string? OperationName { get; set; }
        public bool IsOperationUsed { get; set; }
        public bool HasAccess { get; set; }   // 👈 add this field
    }


}
