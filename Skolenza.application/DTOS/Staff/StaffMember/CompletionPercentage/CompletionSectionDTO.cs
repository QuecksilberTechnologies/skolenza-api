using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.Staff.CompletionPercentage
{

     

    public class CompletionSectionDTO
    {
        public string? SectionName { get; set; }   // Bank, Contact, Experience...
        public double? CompletionPercent { get; set; }       
        public bool? IsInfoVerified { get; set; }
        public bool? IsEditAllowed { get; set; }
        public bool IsSectionCreate { get; set; }
        /// <summary>
        /// Numeric value accepted by StaffMember/update-bulk for this section.
        /// Null means that the section is display-only in the verification dialog.
        /// </summary>
        public int? TabInfoType { get; set; }
        public bool CanUpdateVerificationStatus { get; set; }
    }


    //public class EducationRowDTO
    //{
    //    public string? Degree { get; set; }
    //    public string? InstituteName { get; set; }
    //    public int? ScoreType { get; set; }
    //    public bool? HasEducationDocUploded { get; set; }
    //    public DateTime? StartDate { get; set; }
    //    public DateTime? EndDate { get; set; }
    //    public bool? IsInfoVerified { get; set; }
    //    public bool? IsEditAllowed { get; set; }
    //}


    //public class BankRowDTO
    //{
    //    public string? BankName { get; set; }
    //    public string? AccountNumber { get; set; }
    //    public string? AccountType { get; set; } // e.g., Savings, Current, etc.
    //    public bool? IsPrimaryAccount { get; set; }        
    //    public string? IFSCCode { get; set; }
    //    public string? BranchName { get; set; }      
    //    public bool HasChequeDocUploaded { get; set; }        
    //    public bool? IsInfoVerified { get; set; }
    //    public bool? IsEditAllowed { get; set; }
    //}

}
