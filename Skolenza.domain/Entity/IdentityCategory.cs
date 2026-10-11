using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class IdentityCategory
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public long? AddedById { get; set; }

    public long? UpdatedById { get; set; }

    public DateTime AddedDateTime { get; set; }

    public DateTime? UpdatedDateTime { get; set; }

    public virtual ICollection<IdentityCategoryDocument> IdentityCategoryDocument { get; set; } = new List<IdentityCategoryDocument>();
}
