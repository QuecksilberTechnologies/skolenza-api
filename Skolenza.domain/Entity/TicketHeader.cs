using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class TicketHeader
{
    public long Id { get; set; }

    public long? InstitutionId { get; set; }

    public string HeaderName { get; set; } = null!;

    public int TicketClassificationId { get; set; }

    public bool IsAssetRelated { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public bool IsSoftDeleted { get; set; }

    public long? AddedById { get; set; }

    public DateTime AddedDateTime { get; set; }

    public long? UpdatedById { get; set; }

    public DateTime? UpdatedDateTime { get; set; }

    public long? SoftDeletedById { get; set; }

    public DateTime? SoftDeletedTime { get; set; }

    public virtual Institution? Institution { get; set; }

    public virtual ICollection<Ticket> Ticket { get; set; } = new List<Ticket>();

    public virtual TicketClassification TicketClassification { get; set; } = null!;

    public virtual ICollection<TicketType> TicketType { get; set; } = new List<TicketType>();
}
