using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class Department
{
    public int Id { get; set; }

    public long? InstitutionId { get; set; }

    public string DepartmentName { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsExecutiveOffice { get; set; } = false;

    public bool IsActive { get; set; } = false;

    public string? Remark { get; set; }

    public long? AddedById { get; set; }

    public DateTime? AddedDateTime { get; set; }

    public long? UpdatedById { get; set; }

    public DateTime? UpdatedDateTime { get; set; }

    public bool IsSoftDeleted { get; set; } = false;

    public long? SoftDeletedById { get; set; }

    public DateTime? DeletedDateTime { get; set; }

    public virtual ICollection<Designation> Designation { get; set; } = new List<Designation>();

    public virtual ICollection<StaffMemberManagerMapping> StaffMemberManagerMapping { get; set; } = new List<StaffMemberManagerMapping>();

    public virtual Institution? Institution { get; set; }
}
