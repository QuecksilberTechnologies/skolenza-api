// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the Add StaffMember Manager Mapping DTO data transfer model.
// ================================================================

using skolenza.application.DTOS.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace skolenza.application.DTOS.StaffManagerMappings
{
    /// <summary>
    /// Represents the AddStaffMemberManagerMappingDTO data transfer model.
    /// </summary>
    public class AddStaffMemberManagerMappingDTO
    {
        public long StaffMemberId { get; set; }
        public long ManagerId { get; set; }

        public int ReportingTypeId { get; set; } // Primary / Secondary

        public int? DepartmentId { get; set; }
        public int? DesignationId { get; set; }

        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }

        public string? Description { get; set; }
        public string? Remark { get; set; }

    }
}
