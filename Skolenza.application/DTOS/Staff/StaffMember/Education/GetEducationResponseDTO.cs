
using skolenza.application.DTOS.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.Staff.Education
{
    public class GetEducationResponseDTO
    {

        public int Id { get; set; }
        public string StaffMemberId { get; set; } = string.Empty ;
        public string? Degree { get; set; }

        public string? InstituteName { get; set; }

        public string? Remark { get; set; }

        public DateOnly? StartDate { get; set; }

        public DateOnly? EndDate { get; set; }
        
        public string? ScoreValue { get; set; }
        public string? GradeDivision { get; set; }

        public string? ScoreType { get; set; }
        public string? FilePath { get; set; }

        public int FileType { get; set; }   
        public string? FileName { get; set; }   

        public bool? EducationGap { get; set; }
        public bool? IsEditAllowed { get; set; }
        public bool? IsInfoVerified { get; set; }
        public double GapYears { get; set; }

        public string? ReasonOfEducationGap { get; set; }
 
        public string? InfoVerifiedById { get; set; }

        public bool HasEducationDocUploded { get; set; } 
   
        public bool? IsActive { get; set; }
        public double CompletionPercentage { get; set; }


    }


}
