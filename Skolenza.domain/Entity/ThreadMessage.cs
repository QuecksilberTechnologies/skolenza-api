using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class ThreadMessage
{
    public long Id { get; set; }

    public long ThreadId { get; set; }

    public string Message { get; set; } = null!;

    public int? MessageType { get; set; }

    public long? AddedById { get; set; }

    public DateTime? AddedDateTime { get; set; }

    public bool? IsActive { get; set; }

    public bool? IsSoftDeleted { get; set; }

    public virtual StaffMember? AddedBy { get; set; }

    public virtual TicketThread Thread { get; set; } = null!;
}
