// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the request model for updating Experience.
// ================================================================

using skolenza.application.DTOS.Common;
using skolenza.application.DTOS.Pagination;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace skolenza.application.DTOS.Staff.Experience
{

    // =====================================================================
    //  EXPERIENCE   DTO
    // =====================================================================
    /// <summary>
    /// Represents the UpdateExperienceRequestDTO data transfer model.
    /// </summary>
    public class UpdateExperienceRequestDTO : skolenza.application.DTOs.BaseDTO.PermissionRequestDTO
    {
        //  IMPORTANT
        [Required]
        public string UserStaffMemberId { get; set; } = string.Empty;

        [Required]
        public string StaffMemberId { get; set; } = string.Empty;
        public long Id { get; set; } // null = NEW, value = UPDATE


        //  Parent Fields
        public decimal? Ctc { get; set; }

        //  Job Info
        public string? CompanyName { get; set; }
        public string? PreviousPositionTitle { get; set; }
        public string? StaffMemberIdOfCompany { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public int? Experience { get; set; }
        public bool IsWFH { get; set; }

        //  Location
        public int? WorkingCountryId { get; set; }
        public int? WorkingStateId { get; set; }
        public int? WorkingDistrictId { get; set; }

        public bool IsForeignExperience { get; set; }

        //  Exit
        public string? ReasonForLeaving { get; set; }
        public string? Remark { get; set; }

        //  Reporting
        public string? ColleagueName { get; set; }
        public string? ColleaguePositionTitle { get; set; }
        public string? ColleagueContactNumber { get; set; }

        public string? ReportingManagerName { get; set; }
        public string? ReportingManagerNumber { get; set; }

        public string? VerificationEmail { get; set; }

        //  Gap
        public bool IsAnyGap { get; set; }
        public string? ReasonOfGap { get; set; }

        public DateTime? GapYearFrom { get; set; }
        public DateTime? GapYearTo { get; set; }


        //  Documents
        public List<UpdateExperienceDocumentDTO>? Documents { get; set; }
    }

    // =====================================================================
    //  PAYSLIP UPLOAD DTO
    // =====================================================================
    /// <summary>
    /// Represents the UpdateExperienceDocumentDTO data transfer model.
    /// </summary>
    public class UpdateExperienceDocumentDTO
    {
        //  IMPORTANT
        public long? Id { get; set; } // null = NEW, value = UPDATE
        public IFormFile? File { get; set; }

        public int DocumentType { get; set; }

        public string? FileName { get; set; }
        public string? FilePath { get; set; }

        public string? Remark { get; set; }
    }

}
