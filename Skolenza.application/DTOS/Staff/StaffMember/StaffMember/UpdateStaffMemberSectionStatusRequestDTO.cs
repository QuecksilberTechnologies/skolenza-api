// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the request model for updating StaffMember Section Status.
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
    /// Represents the UpdateStaffMemberSectionStatusRequestDTO data transfer model.
    /// </summary>
    public class UpdateStaffMemberSectionStatusRequestDTO : skolenza.application.DTOs.BaseDTO.PermissionRequestDTO
    {
       
        public required string StaffMemberId { get; set; }   
        public bool? IsActive { get; set; }


        public List<SectionStatusDTO>? Sections { get; set; }
    }

    /// <summary>
    /// Represents the SectionStatusDTO data transfer model.
    /// </summary>
    public class SectionStatusDTO
    {
        public required int TabInfoType { get; set; } // "education", "bank", "experience"
        public bool? IsVerified { get; set; }
        public bool? IsEditAllowed { get; set; }

        // Optional: Primary key for that section (EducationId, BankId)
      //   public string? StaffMemberId { get; set; }
    
    
    }

}
