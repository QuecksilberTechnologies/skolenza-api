using skolenza.application.DTOs.Role;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.UserLogin
{
    public class AccessDetailRequestDTO
    {
        public long StaffMemberId { get; set; } // StaffMember requesting the menu         
        public int StaffEmploymentTypeId { get; set; } // StaffMember requesting the menu         
        public int? ForPlatform { get; set; } // 1: Web, 2: Mobile
        public bool IncludeInactive { get; set; } = false; // Include inactive menus
        public int? ParentMenuId { get; set; } // Optional: Fetch submenus of a specific m
        public IEnumerable<RoleInfoDTO>? roleInfo { get; set; } = null; 
        
    }

}
