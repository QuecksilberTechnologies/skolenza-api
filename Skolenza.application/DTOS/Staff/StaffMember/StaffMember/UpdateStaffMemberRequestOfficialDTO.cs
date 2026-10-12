// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the Update StaffMember Request Official DTO data transfer model.
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
    public class UpdateStaffMemberRequestOfficialDTO : skolenza.application.DTOs.BaseDTO.PermissionRequestDTO
    {
        [Required]
      
        public string StaffMemberId { get; set; } = default!;

        // ---- Official / Admin controlled ----
        public string? FirstName { get; set; }

        [MaxLength(100)]
        public string? MiddleName { get; set; }
        [MaxLength(100)]
        public string? LastName { get; set; }
        [MaxLength(100)]
        public string? Description { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public int? DesignationId { get; set; }
        public bool? IsMarried { get; set; }
        public string? EmergencyContactNumber { get; set; }
        public string? EmergencyContactPerson { get; set; }
        public string? BloodGroup { get; set; }
        public string? MobileNumber { get; set; }
        public int? Relation { get; set; }
        public int? CountryId { get; set; }
        public int? DepartmentId { get; set; }
        public int? StaffEmploymentTypeId { get; set; }
        public short? StaffCategoryId { get; set; }
        public int? StaffPositionMasterId { get; set; }
        public int? GenderId { get; set; }           
        public DateTime? DateOfOnBoarding { get; set; }
        public DateTime? DateOfExit { get; set; }
        public bool? HasPermanent { get; set; }
        public bool? IsEditAllowed { get; set; }
        public bool? IsInfoVerified { get; set; }
        public bool? IsActive { get; set; }
        public string? Remark { get; set; }
 
    }

}
