using skolenza.application.DTOS.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.Staff.Contact
{
   
        public class GetContactResponseDTO
    {
           public required long Id { get; set; }
            public string? StaffMemberId { get; set; }          // e.g. "Mobile", "Office", "Home"
            public string? ContactName { get; set; }          // e.g. "Mobile", "Office", "Home"            
            public int? Relation { get; set; }          // e.g. "Mobile", "Office", "Home"            
            public int? ContactType { get; set; }          // e.g. "Mobile", "Office", "Home"            
            public string? ContactNumber { get; set; } 
            public string? AlternateNumber { get; set; } = string.Empty;      // Optional alternate number
            public  string? Email { get; set; } = string.Empty;               // Email ID
            public bool IsPrimary { get; set; }              // Is this the primary contact?
            public string? CountryName { get; set; }               // Country
            public string? StateName { get; set; }               // Country
            public string? DistrictName { get; set; }               // Country
            public string? LocalityName { get; set; }
            public int? CountryId { get; set; }               // Country
            public int? StateId { get; set; }                // State
            public int? DistrictId { get; set; }             // District
            public int? LocalityId { get; set; }
            public string? HouseNo { get; set; }              // Address details
            public string? LandMark { get; set; }
            public string? Street { get; set; }
           
            public string? Address { get; set; }
            public string? Remark { get; set; }
            public bool? IsActive { get; set; }               // Active/Inactive
            
         
            public bool? IsEditAllowed { get; set; }         // can edit contact?
            public bool? IsInfoVerified { get; set; }        // verified or not
            public string? InfoVerifiedById { get; set; }
            public DateTime? InfoVerifiedDateTime { get; set; }
            public string? Description { get; set; }          // extra notes / description
        public double CompletionPercentage { get; set; } = 0;
    }
    

}
