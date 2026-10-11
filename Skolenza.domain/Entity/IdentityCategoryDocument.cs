using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class IdentityCategoryDocument
{
    public int Id { get; set; }

    public int IdentityCategoryId { get; set; }

    public string Code { get; set; } = null!;

    public string DocumentName { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsUnique { get; set; }

    public bool IsActive { get; set; }

    public long? AddedById { get; set; }

    public long? UpdatedById { get; set; }

    public DateTime AddedDateTime { get; set; }

    public DateTime? UpdatedDateTime { get; set; }

    public virtual ICollection<CountryIdentityRule> CountryIdentityRule { get; set; } = new List<CountryIdentityRule>();

    public virtual ICollection<StaffMemberIdentity> StaffMemberIdentity { get; set; } = new List<StaffMemberIdentity>();

    public virtual IdentityCategory IdentityCategory { get; set; } = null!;
}
