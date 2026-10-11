using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class Country
{
    public int Id { get; set; }

    public string CountryName { get; set; } = null!;

    public string? CountryCode { get; set; }

    public bool? IsActive { get; set; }

    public string Stdcode { get; set; } = null!;

    public virtual ICollection<ComplianceRule> ComplianceRule { get; set; } = new List<ComplianceRule>();

    public virtual ICollection<CountryIdentityRule> CountryIdentityRule { get; set; } = new List<CountryIdentityRule>();

    public virtual ICollection<CountryStatutoryRule> CountryStatutoryRule { get; set; } = new List<CountryStatutoryRule>();

    public virtual ICollection<StaffMember> StaffMember { get; set; } = new List<StaffMember>();

    public virtual ICollection<StaffMemberTaxProfile> StaffMemberTaxProfile { get; set; } = new List<StaffMemberTaxProfile>();


    public virtual ICollection<SalaryComponentMaster> SalaryComponentMaster { get; set; } = new List<SalaryComponentMaster>();

    public virtual ICollection<State> State { get; set; } = new List<State>();

    public virtual ICollection<StatutoryType> StatutoryType { get; set; } = new List<StatutoryType>();

    public virtual ICollection<TaxRegimeMaster> TaxRegimeMaster { get; set; } = new List<TaxRegimeMaster>();

    public virtual ICollection<TaxRule> TaxRule { get; set; } = new List<TaxRule>();

    public virtual ICollection<TaxSlab> TaxSlab { get; set; } = new List<TaxSlab>();

    public virtual ICollection<TaxSystemMaster> TaxSystemMaster { get; set; } = new List<TaxSystemMaster>();
}
