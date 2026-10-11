using System;
using System.Collections.Generic;
using System.Text;

namespace skolenza.application.DTOS.Staff.Dependent
{
    public class GetEnrolledStaffMemberRequestDTO : skolenza.application.DTOs.BaseDTO.PermissionRequestDTO
    {
        public required string StaffMemberId { get; set; }  // 🔐 Encoded
    }
}
