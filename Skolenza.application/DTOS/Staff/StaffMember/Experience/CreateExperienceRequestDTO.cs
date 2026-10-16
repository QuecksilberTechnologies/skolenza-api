// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the request model for creating Experience.
// ================================================================

using skolenza.application.DTOS.Common;
using skolenza.application.DTOS.Pagination;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;

namespace skolenza.application.DTOS.Staff.Experience
{
    // =====================================================================
    //  CREATE EXPERIENCE REQUEST DTO
    // =====================================================================

    /// <summary>
    /// Represents the CreateExperienceRequestDTO data transfer model.
    /// </summary>
    public class CreateExperienceRequestDTO : skolenza.application.DTOs.BaseDTO.PermissionRequestDTO
    {
        public required string StaffMemberId { get; set; }
        public decimal? Ctc { get; set; }
        //  Job Info
        public string? CompanyName { get; set; }
        public string? PreviousPositionTitle { get; set; }
        public string? StaffMemberIdOfCompany { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public int? Experience { get; set; }
        public bool IsWFH { get; set; } = false;

        //  Location
        public int? WorkingCountryId { get; set; }
        public int? WorkingStateId { get; set; }
        public int? WorkingDistrictId { get; set; }

        public bool IsForeignExperience { get; set; } = false;

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
        public bool IsAnyGap { get; set; } = false;
        public string? ReasonOfGap { get; set; }

        public DateTime? GapYearFrom { get; set; }
        public DateTime? GapYearTo { get; set; }

        //  Documents ( Important)
        public List<CreateExperienceDocumentDTO>? Documents { get; set; }
    }

    // =====================================================================
    //  PAYSLIP UPLOAD DTO
    // =====================================================================
    /// <summary>
    /// Represents the CreateExperienceDocumentDTO data transfer model.
    /// </summary>
    public class CreateExperienceDocumentDTO
    {
        public int DocumentType { get; set; }   // ENUM use hoga

        public string? FileName { get; set; }
        public IFormFile? File { get; set; }

        public string? Remark { get; set; }
    }

}
