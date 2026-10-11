using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class Ticket
{
    public long Id { get; set; }

    public long InstitutionId { get; set; }

    public string TicketNumber { get; set; } = null!;

    public int? TicketClassificationId { get; set; }

    public long TicketHeaderId { get; set; }

    public long TicketTypeId { get; set; }

    public string? Description { get; set; }

    public int Priority { get; set; }

    public int Status { get; set; }

    public int? AssignedToRoleId { get; set; }

    public long? AssignedToUserId { get; set; }

    public long? RequestedForUserId { get; set; }

    public long RequestedByUserId { get; set; }

    public long? RecommendedByUserId { get; set; }

    public bool? IsApproved { get; set; }

    public long? ApprovedByUserId { get; set; }

    public DateTime? ApprovedDateTime { get; set; }

    public string? RejectedReason { get; set; }

    public int? SLAHoursSnapshot { get; set; }

    public DateTime? SLAStartTime { get; set; }

    public DateTime? SLAEndTime { get; set; }

    public bool? IsSLABreached { get; set; }

    public DateTime? ResolvedDateTime { get; set; }

    public DateTime? ClosedDateTime { get; set; }

    public int? ReopenedCount { get; set; }

    public int? EscalationLevel { get; set; }

    public long? EscalatedToUserId { get; set; }

    public DateTime? EscalatedOn { get; set; }

    public DateTime? DueDateTime { get; set; }

    public long AddedById { get; set; }

    public DateTime AddedDateTime { get; set; }

    public long? UpdatedById { get; set; }

    public DateTime? UpdatedDateTime { get; set; }

    public bool IsActive { get; set; }

    public bool IsSoftDeleted { get; set; }

 
    public virtual StaffMember? ApprovedByUser { get; set; }

    public virtual Role? AssignedToRole { get; set; }

    public virtual StaffMember? AssignedToUser { get; set; }

    public virtual StaffMember? RecommendedByUser { get; set; }

    public virtual StaffMember? RequestedByUser { get; set; }

    public virtual StaffMember? RequestedForUser { get; set; }

    public virtual Institution Institution { get; set; } = null!;

    public virtual TicketClassification? TicketClassification { get; set; }
    public virtual ICollection<TicketHistory> TicketHistory { get; set; } = new List<TicketHistory>();


    public virtual TicketHeader TicketHeader { get; set; } = null!;

    public virtual TicketType TicketType { get; set; } = null!;

 
}
