// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the request model for updating StaffMember.
// ================================================================

using skolenza.application.DTOS.Common;
using skolenza.application.DTOS.Pagination;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace skolenza.application.DTOS.Staff.BaseStaffMember
{
    /// <summary>
    /// StaffMember create request
    /// </summary>
    public class UpdateStaffMemberRequestDTO : skolenza.application.DTOs.BaseDTO.PermissionRequestDTO
    {

      
        [Required]
        public string StaffMemberId { get; set; } = string.Empty;

        [MaxLength(100)]
      
        public string? FirstName { get; set; }

   
        [MaxLength(100)]
        public string? MiddleName { get; set; }
        [MaxLength(100)]
        public string? LastName { get; set; }      
        public DateTime? DateOfBirth { get; set; }
        public bool? IsMarried { get; set; }
        public string? EmergencyContactNumber { get; set; }
        public string? EmergencyContactPerson { get; set; }
        public string? BloodGroup { get; set; }
        public string? MobileNumber { get; set; }
        public int? Relation { get; set; }
        public int? GenderId { get; set; }     

        [MaxLength(300)]
        public string? Remark { get; set; }
 
      
        public string? Description { get; set; }

    }

}
