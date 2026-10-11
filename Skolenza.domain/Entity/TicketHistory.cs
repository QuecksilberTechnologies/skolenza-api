using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class TicketHistory
{
    public long Id { get; set; }

    public long TicketId { get; set; }

    public string Action { get; set; } = null!;

    public int? OldStatus { get; set; }

    public int? NewStatus { get; set; }

    public long? OldAssignedToUserId { get; set; }

    public long? NewAssignedToUserId { get; set; }

    public int? OldAssignedToRoleId { get; set; }

    public int? NewAssignedToRoleId { get; set; }

    public string? Remarks { get; set; }

    public long DoneByUserId { get; set; }

    public DateTime DoneOn { get; set; }

    public long InstitutionId { get; set; }

    public bool IsActive { get; set; }

    public bool IsSoftDeleted { get; set; }

    public virtual StaffMember DoneByUser { get; set; } = null!;

    public virtual Ticket Ticket { get; set; } = null!;
}
