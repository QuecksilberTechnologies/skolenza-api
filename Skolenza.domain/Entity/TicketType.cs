using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

 
public partial class TicketType
{
    public long Id { get; set; }

    public string TicketTypeName { get; set; } = null!;

    // 🔥 REQUIRED (Fix)
    public long TicketHeaderId { get; set; }

    // 🔥 REQUIRED (Fix)
    public long InstitutionId { get; set; }

    // 🔥 REQUIRED (Fix)
    public int ResponsibleRoleId { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsSoftDeleted { get; set; } = false;

    public long AddedById { get; set; }

    public DateTime AddedDateTime { get; set; }

    public long? UpdatedById { get; set; }

    public DateTime? UpdatedDateTime { get; set; }

    public long? SoftDeletedById { get; set; }

    public DateTime? SoftDeletedTime { get; set; }

    // 🔥 Approval Engine
    public bool IsApprovalRequired { get; set; } = false;

    public int? ApprovalRoleId { get; set; }

    public bool AutoApproveIfSameRole { get; set; } = false;

    // 🔥 SLA (flexible)
    public int? SLAHours { get; set; }

    public bool IsActiveForAllUsers { get; set; } = true;
    public bool IsAttachmentRequired { get; set; } = false;

    // 🔗 Navigation
    public virtual Role? ApprovalRole { get; set; }

    public virtual Role ResponsibleRole { get; set; } = null!;

    public virtual Institution Institution { get; set; } = null!;

    public virtual TicketHeader TicketHeader { get; set; } = null!;

    public virtual ICollection<Ticket> Ticket { get; set; } = new List<Ticket>();
}