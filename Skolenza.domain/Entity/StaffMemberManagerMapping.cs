using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class StaffMemberManagerMapping
{
    public long Id { get; set; }

    public long InstitutionId { get; set; }

    public long StaffMemberId { get; set; }

    public long ManagerId { get; set; }

    public int? DepartmentId { get; set; }

    public int? StaffPositionMasterId { get; set; }

    public int ReportingTypeId { get; set; }

    public DateTime EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }

    public bool IsActive { get; set; }

    public string? Description { get; set; }

    public string? Remark { get; set; }

    public long? AddedById { get; set; }

    public DateTime? AddedDateTime { get; set; }

    public long? UpdatedById { get; set; }

    public DateTime? UpdatedDateTime { get; set; }

    public bool? IsSoftDeleted { get; set; }

    public virtual Department? Department { get; set; }

    public virtual StaffPositionMaster? StaffPositionMaster { get; set; }

    public virtual StaffMember StaffMember { get; set; } = null!;

    public virtual StaffMember Manager { get; set; } = null!;

    public virtual ReportingType ReportingType { get; set; } = null!;

    public virtual Institution Institution { get; set; } = null!;
}
