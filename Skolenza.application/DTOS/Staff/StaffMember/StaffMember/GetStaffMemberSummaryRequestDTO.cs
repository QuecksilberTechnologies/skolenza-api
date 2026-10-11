// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the request model for retrieving StaffMember Summary.
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
    /// Represents the GetStaffMemberSummaryRequestDTO data transfer model.
    /// </summary>
    public class GetStaffMemberSummaryRequestDTO : skolenza.application.DTOs.BaseDTO.PermissionRequestDTO
    {
        public required string StaffMemberId { get; set; }
        public required bool IsActive { get; set; }
    }
}
