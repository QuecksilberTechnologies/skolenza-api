using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.Role
{
   
    public class GetRoleOptionResponseDTO
    {

        public int Id { get; set; }
        public int? RoleType { get; set; }
        public string? RoleName { get; set; } = string.Empty;
        public bool IsActive { get; set; }

    }
}
