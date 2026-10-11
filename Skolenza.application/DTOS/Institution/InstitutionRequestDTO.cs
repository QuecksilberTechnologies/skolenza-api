using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.Institution
{
  
        public class InstitutionRequestDTO
         {
            public long StaffMemberId { get; set; }
            
            public int RoleId { get; set; }
            public int SubscriptionPlanId { get; set; }
            public int DesignationId { get; set; }
            public long? Id { get; set; }
            public string? CompanyName { get; set; } 

           public string? InstitutionCode { get; set; }

           public string? CompanyEmailDomain { get; set; } 

           public string? InstitutionEmail { get; set; }    

           public string? ContactPersonName { get; set; }

           public string? ContactNumber { get; set; }

           public int? CountryId { get; set; }

           public bool? IsVerified { get; set; }

           public bool? IsActive { get; set; }


    }

}
