using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class TicketAttachment
{
    public long Id { get; set; }

    public long TicketId { get; set; }

    public string? FileName { get; set; }

    public string? FilePath { get; set; }

    public string? FileType { get; set; }

    public long? FileSize { get; set; }

    public long? UploadedByUserId { get; set; }

    public DateTime? UploadedDateTime { get; set; }

    public bool? IsActive { get; set; }

    public bool? IsSoftDeleted { get; set; }

    public DateTime? DeletedDateTime { get; set; }

    public virtual Ticket Ticket { get; set; } = null!;

    public virtual StaffMember? UploadedByUser { get; set; }
}
