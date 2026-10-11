using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class TicketThread
{
    public long Id { get; set; }

    public int EntityType { get; set; }

    public long EntityId { get; set; }

    public long? InstitutionId { get; set; }

    public bool? IsActive { get; set; }

    public bool? IsSoftDeleted { get; set; }

    public long? AddedById { get; set; }

    public DateTime? AddedDateTime { get; set; }

    public virtual ICollection<ThreadMessage> ThreadMessage { get; set; } = new List<ThreadMessage>();
}
