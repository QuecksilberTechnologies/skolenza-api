// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the User Role DTO data transfer model.
// ================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using skolenza.application.DTOS.Common;
using skolenza.domain.Entity;
using MediatR;

namespace skolenza.application.DTOS.UserRoles 
{
    /// <summary>
    /// Represents the UserRoleDTO data transfer model.
    /// </summary>
    public class UserRoleDTO
    {  
       
        public string? RoleName { get; set; }
        public long UserRoleId { get; set; }
        public int RoleId { get; set; }   
        public bool IsActive { get; set; }             
        public bool IsPrimaryRole { get; set; }  =false;           
        public int RoleType { get; set; }
        
    
       
    } 
   /// <summary>
   /// Represents the UserRoleListDTO data transfer model.
   /// </summary>
   public class UserRoleListDTO 
    {
        public required string StaffMemberId { get; set; } 
        public List<UserRoleDTO> UserRoles { get; set; } = new List<UserRoleDTO>();
    }


}
