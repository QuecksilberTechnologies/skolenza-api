using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.SubscriptionModule
{
    public class GetActiveInstitutionSubscriptionDetailResquestDTO
    {
        
            public long? InstitutionId { get; set; }  // ✅ This is enough
        }

     
     
}
