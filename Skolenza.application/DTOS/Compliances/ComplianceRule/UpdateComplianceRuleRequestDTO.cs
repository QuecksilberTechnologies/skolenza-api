// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the request model for updating Compliance Rule.
// ================================================================

using skolenza.application.DTOS.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace skolenza.application.DTOS.Compliances.ComplianceRule
{

    /// <summary>
    /// Represents the UpdateComplianceRuleRequestDTO data transfer model.
    /// </summary>
    public class UpdateComplianceRuleRequestDTO
    {
        public long Id { get; set; }
        public int ComplianceTypeId { get; set; }
        public int CountryId { get; set; }
        public int? StateId { get; set; }
        public object RuleJson { get; set; }
        public int Priority { get; set; }
        public DateOnly EffectiveFrom { get; set; }
        public DateOnly? EffectiveTo { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
