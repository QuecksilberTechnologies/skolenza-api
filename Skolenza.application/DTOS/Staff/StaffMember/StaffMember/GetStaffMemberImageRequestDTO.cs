// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the request model for retrieving StaffMember Image.
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
    /// Represents the GetStaffMemberImageRequestDTO data transfer model.
    /// </summary>
    public class GetStaffMemberImageRequestDTO : skolenza.application.DTOs.BaseDTO.PermissionRequestDTO
    {

        public string? StaffMemberId { get; set; } 
        public bool IsActive { get; set; } =true;


    }
}
