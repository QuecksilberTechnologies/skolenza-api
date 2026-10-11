using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class UserRole
{
    public long Id { get; set; }

    public long? StaffMemberId { get; set; }

    public int? RoleId { get; set; }

    public bool IsActive { get; set; }

    public bool? IsPrimaryRole { get; set; }

    public string? Remark { get; set; }

    public DateTime? AssignedDateTime { get; set; }

    public DateTime? RemovedDateTime { get; set; }

    public long? AssignedById { get; set; }

    public DateTime? RoleStartDate { get; set; }

    public bool? ApprovalRequired { get; set; }

    public string? ApprovalStatus { get; set; }

    public long AddedById { get; set; }

    public DateTime AddedDateTime { get; set; }

    public long? UpdatedById { get; set; }

    public DateTime? UpdatedDateTime { get; set; }

    public bool? IsSoftDeleted { get; set; }

    public long? SoftDeletedById { get; set; }

    public DateTime? DeletedDateTime { get; set; }

    public virtual StaffMember? StaffMember { get; set; }

    public virtual Role? Role { get; set; }
}
