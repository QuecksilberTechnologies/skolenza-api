using skolenza.application.DTOs.BasicAndRoleBaseMenu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.UserLogin
{
    public class AccessDetailResponseDTO
    {
        public long StaffMemberId { get; set; }
        public int? ForPlatform { get; set; }
        public IEnumerable<BasicMenuDTO>? BasicMenus { get; set; } = new List<BasicMenuDTO>();
        public IEnumerable<UserRolesPermissionOnModuleDTO>? UserRolesPermissionOnModule { get; set; } = new List<UserRolesPermissionOnModuleDTO>();
    }

}
