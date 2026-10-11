using skolenza.application.DTOS.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.RoleModulePermission
{
    public class GetActiveRoleModuleOperationsRequestDTO 
    {
        
        public long  InstitutionId { get; set; }
        public string RoleIds { get; set; } = string.Empty; // comma-separated: "1,2,3"
    }

}
