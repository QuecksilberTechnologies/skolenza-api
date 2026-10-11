using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class LeaveRequest
{
    public long Id { get; set; }

    public long InstitutionId { get; set; }

    public long StaffMemberId { get; set; }

    public int LeaveTypeId { get; set; }

    public DateOnly FromDate { get; set; }

    public DateOnly ToDate { get; set; }

    public bool? IsHalfDay { get; set; }

    public DateOnly? HalfDayDate { get; set; }

    public bool? IsFirstHalf { get; set; }

    public decimal TotalLeaveDays { get; set; }

    public string? Reason { get; set; }

    public int Status { get; set; }

    public long? ApprovedById { get; set; }

    public DateTime? ApprovedDate { get; set; }

    public bool? IsSandwich { get; set; }

    public long CreatedById { get; set; }

    public DateTime? CreatedDateTime { get; set; }

    public DateTime? CancellationDate { get; set; }

    public long? UpdatedById { get; set; }

    public DateTime? UpdatedDateTime { get; set; }

    public string? Remark { get; set; }

    public bool? IsDocumentAttached { get; set; }

    public virtual StaffMember StaffMember { get; set; } = null!;


    public virtual LeaveType LeaveType { get; set; } = null!;

    public virtual Institution Institution { get; set; } = null!;
}
