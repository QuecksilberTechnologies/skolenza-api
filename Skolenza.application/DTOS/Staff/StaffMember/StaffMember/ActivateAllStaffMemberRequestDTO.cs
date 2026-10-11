// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the request model for processing Activate All StaffMember.
// ================================================================

using skolenza.application.DTOS.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.Staff.BaseStaffMember
{
    /// <summary>
    /// Represents the ActivateAllStaffMemberRequestDTO data transfer model.
    /// </summary>
    public class ActivateAllStaffMemberRequestDTO : skolenza.application.DTOs.BaseDTO.PermissionRequestDTO
    {
        public string? UserStaffMemberId { get; set; }
        public string? StaffMemberId { get; set; }
        public long? Id { get; set; }
        public required bool IsActive { get; set; }
    }

}
