using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class ComplianceTypeMaster
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int? CountryId { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<ComplianceRule> ComplianceRule { get; set; } = new List<ComplianceRule>();

    public virtual ICollection<SalaryComponentMaster> SalaryComponentMaster { get; set; } = new List<SalaryComponentMaster>();
}
