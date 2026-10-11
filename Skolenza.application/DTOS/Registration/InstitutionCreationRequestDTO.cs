using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.application.DTOs.BaseDTO; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.Registration
{
    public class InstitutionCreationRequestDTO : PermissionRequestDTO
    {
       
        public int SubscriptionPlanId { get; set; } 

        public int InstitutionTypeId { get; set; } 
        public string CompanyName { get; set; } = null!;

        public string InstitutionCode { get; set; } = null!;
        //public required string Prefix { get; set; } = null!;
        //public bool IncludeYear { get; set; } = true;
        //public bool IncludeMonth { get; set; } = true;
        //public bool IncludeDepartment { get; set; } = true;
        //public required string Separator { get; set; } = null!;
        //public required string RunningNumberLength { get; set; } = null!; 
        public string Prefix { get; set; } = "EMP";
        public bool IncludeYear { get; set; } = true;
        public bool IncludeMonth { get; set; } = true;
        public bool IncludeDepartment { get; set; } = false;
        public string Separator { get; set; } = "/";
        public string RunningNumberLength { get; set; } = "4";
        public string CompanyEmailDomain { get; set; } = null!;
        public int GenderId { get; set; } 
         
        public string InstitutionEmail { get; set; } = null!;

        public string? ContactPersonName { get; set; }

        public string? ContactNumber { get; set; }
      
        public int CountryId { get; set; }     
 
 
    }

}
