using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class StatutoryType
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public int CountryId { get; set; }

    public bool IsStaffMemberContributionRequired { get; set; }

    public bool IsEmployerContributionRequired { get; set; }

    public bool IsActive { get; set; }

    public long? AddedById { get; set; }

    public long? UpdatedById { get; set; }

    public long? DeletedById { get; set; }

    public DateTime AddedDateTime { get; set; }

    public DateTime? UpdatedDateTime { get; set; }

    public DateTime? DeletedDateTime { get; set; }

    public virtual Country Country { get; set; } = null!;

    public virtual ICollection<CountryStatutoryRule> CountryStatutoryRule { get; set; } = new List<CountryStatutoryRule>();

    public virtual ICollection<StaffMemberStatutoryAccount> StaffMemberStatutoryAccount { get; set; } = new List<StaffMemberStatutoryAccount>();
}
