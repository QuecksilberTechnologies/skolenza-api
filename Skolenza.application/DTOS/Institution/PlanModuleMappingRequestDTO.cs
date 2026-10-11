using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.Institution
{
    public class PlanModuleMappingRequestDTO
     {
        
            public long? InstitutionId { get; set; }
            public long StaffMemberId { get; set; }
            public int SubscriptionPlanId { get; set; }            
            public bool? IsActive { get; set; }
            public string? Remark { get; set; }
         
        }
  
}
