// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the request model for creating StaffMember Image.
// ================================================================

using skolenza.application.DTOS.Common;
using skolenza.application.DTOS.Pagination;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.Staff.BaseStaffMember
{
 
    /// <summary>
    /// Represents the CreateStaffMemberImageRequestDTO data transfer model.
    /// </summary>
    public class CreateStaffMemberImageRequestDTO : skolenza.application.DTOs.BaseDTO.PermissionRequestDTO
    {
         
        public required bool IsActive { get; set; }      
        public IFormFile? ImageFile { get; set; }

    }
}
