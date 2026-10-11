using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.Role
{

    public class UpdateRoleDTO
    {
        public int Id { get; set; } // Required for update

        public long StaffMemberId { get; set; } // Should not be nullable unless you're using it only from context
        public int RoleId { get; set; } // Should not be nullable unless you're using it only from context
        public long InstitutionId { get; set; } // Should not be nullable unless you're using it only from context

        public string RoleName { get; set; } = string.Empty;

        public string? Remark { get; set; }

        public bool IsActive { get; set; } = false;        
         
    }

}
