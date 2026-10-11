using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;
 
public partial class State
{
    public int Id { get; set; }

    public string StateName { get; set; } = null!;

    public string StateCode { get; set; } = null!;

    public int CountryId { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<Locality> Localities { get; set; } = new List<Locality>();

    public virtual ICollection<ComplianceRule> ComplianceRule { get; set; } = new List<ComplianceRule>();

    public virtual Country Country { get; set; } = null!;

    public virtual ICollection<District> District { get; set; } = new List<District>();

    public virtual ICollection<StaffMemberTaxProfile> StaffMemberTaxProfile { get; set; } = new List<StaffMemberTaxProfile>();

    public virtual ICollection<SalaryComponentMaster> SalaryComponentMaster { get; set; } = new List<SalaryComponentMaster>();

    public virtual ICollection<TaxSlab> TaxSlab { get; set; } = new List<TaxSlab>();
}

