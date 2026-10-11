using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class TenderStatus
{
    public int Id { get; set; }

    public string StatusName { get; set; } = null!;

    public string? Description { get; set; }

    public string? Remark { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<Tender> Tender { get; set; } = new List<Tender>();
}
