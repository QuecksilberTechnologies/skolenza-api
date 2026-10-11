using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.Institution
{

    public class InstitutionSubscriptionPlanRequestDTO
    {
        public long StaffMemberId { get; set; }
        public int RoleId { get; set; }
        public long InstitutionId { get; set; }
        public int? Id { get; set; }      
        public int? SubscriptionPlanId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsTrial { get; set; }


    }
}
