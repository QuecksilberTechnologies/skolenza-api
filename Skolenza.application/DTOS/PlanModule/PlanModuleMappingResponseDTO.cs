using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.PlanModule
{
    public class PlanModuleMappingResponseDTO
    {
        public int Id { get; set; }
        public int SubscriptionPlanId { get; set; }
        public string? SubscriptionPlanName { get; set; }

        public int ModuleId { get; set; }
        public string? ModuleName { get; set; }

        public bool? IsActive { get; set; }
        public string? Remark { get; set; }

        public DateTime? AddedDateTime { get; set; }
        public DateTime? UpdatedDateTime { get; set; }
    }

}
