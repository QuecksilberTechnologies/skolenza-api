// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the request model for updating StaffMember Image.
// ================================================================

using skolenza.application.DTOS.Common;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.Staff.BaseStaffMember
{
    /// <summary>
    /// Represents the UpdateStaffMemberImageRequestDTO data transfer model.
    /// </summary>
    public class UpdateStaffMemberImageRequestDTO : skolenza.application.DTOs.BaseDTO.PermissionRequestDTO
    {
        
        public long Id { get; set; }
        public IFormFile? ProfileImage { get; set; }
        public string? FileName { get; set; }
        public required bool IsActive { get; set; }
        

    }
}
