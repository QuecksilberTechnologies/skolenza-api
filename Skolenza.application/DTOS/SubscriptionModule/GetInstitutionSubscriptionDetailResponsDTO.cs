using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.SubscriptionModule
{
    public class GetInstitutionSubscriptionDetailResponsDTO 
    {
        public long? InstitutionId { get; set; }
        public string? PlanName { get; set; }
        public int? InstitutionSubscriptionId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; }

    }
}
