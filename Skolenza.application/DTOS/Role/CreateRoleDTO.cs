using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.Role
{
    public class CreateRoleDTO
    {

        public long? InstitutionId { get; set; }
        public long StaffMemberId { get; set; }
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty; // Default value        
        public string? Remark { get; set; } // Nullable
        public bool IsActive { get; set; } = false; // Default false
 





    }
}
