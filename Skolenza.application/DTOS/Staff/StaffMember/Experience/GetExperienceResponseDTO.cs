using skolenza.application.Common.Helpers;
using skolenza.application.DTOS.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.Staff.Experience
{
    

    public class GetStaffMemberExperienceResponseDTO 
    {

      
        public long Id { get; set; }
        public string? StaffMemberId { get; set; }

        // 🔹 Basic Info
        public decimal? Ctc { get; set; }


        // 🔹 Job Info
        public string? CompanyName { get; set; }
        public string? PreviousPositionTitle { get; set; }
        public string? StaffMemberIdOfCompany { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public int? Experience { get; set; }
        public bool IsWFH { get; set; }

        // 🔹 Location
        public int? WorkingCountryId { get; set; }
        public int? WorkingStateId { get; set; }
        public int? WorkingDistrictId { get; set; }

        public bool IsForeignExperience { get; set; }

        // 🔹 Exit
        public string? ReasonForLeaving { get; set; }
        public string? Remark { get; set; }

        // 🔹 Reporting
        public string? ColleagueName { get; set; }
        public string? ColleaguePositionTitle { get; set; }
        public string? ColleagueContactNumber { get; set; }

        public string? ReportingManagerName { get; set; }
        public string? ReportingManagerNumber { get; set; }

        public string? VerificationEmail { get; set; }
        public double CompletionPercentage { get; set; }

        // 🔹 Gap
        public bool IsAnyGap { get; set; }
        public string? ReasonOfGap { get; set; }

        public DateTime? GapYearFrom { get; set; }
        public DateTime? GapYearTo { get; set; }

        // 🔹 Verification
        public bool? IsExperienceVerified { get; set; }
        public bool? IsExperienceVerifiedByMail { get; set; }
        public bool? IsExperienceVerifiedByCall { get; set; }

        public bool? IsInfoVerified { get; set; }
        public bool? IsEditAllowed { get; set; }

        // 🔹 Documents
        public List<GetStaffMemberExperienceDocumentDTO>? Documents { get; set; }
    }
    public class GetStaffMemberExperienceDocumentDTO
    {
        public long Id { get; set; }

        public int DocumentType { get; set; }
        public string? DocumentTypeName { get; set; }  

        public string? FileName { get; set; }
        public string? FilePath { get; set; }
        public bool HasExperienceDocUploaded { get; set; }
        public string? Url { get; set; } // 🔥 Ready URL

        public string? Remark { get; set; }
    }

}
