using System;
using System.Collections.Generic;
using System.Text;

namespace skolenza.application.DTOS.Compliances.ComplianceRule
{
    public class GetComplianceRuleResponseDTO
    {
        public long Id { get; set; }
        public int ComplianceTypeId { get; set; }
        public int CountryId { get; set; }
        public int? StateId { get; set; }

        public object RuleJson { get; set; }

        public int Priority { get; set; }

        public long? InstitutionId { get; set; }

        public DateOnly EffectiveFrom { get; set; }
        public DateOnly? EffectiveTo { get; set; }       
        public DateTime UpdatedDateTime { get; set; }
        public long UpdatedById  { get; set; }
 
    }
}
