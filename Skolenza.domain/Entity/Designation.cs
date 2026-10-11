using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class Designation
{
    public int Id { get; set; }

    public long InstitutionId { get; set; }

    public int DepartmentId { get; set; }

    public string DesignationName { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = false;

    public long? AddedById { get; set; }

    public DateTime? AddedDateTime { get; set; }

    public long? UpdatedById { get; set; }

    public DateTime? UpdatedDateTime { get; set; }

    public bool IsSoftDeleted { get; set; } = false;

    public DateTime? SoftDeletedDateTime { get; set; }

    public long? SoftDeletedById { get; set; }


    public virtual Department? Department { get; set; }

    public virtual ICollection<StaffMember> StaffMember { get; set; } = new List<StaffMember>();

    public virtual ICollection<StaffMemberManagerMapping> StaffMemberManagerMapping { get; set; } = new List<StaffMemberManagerMapping>();


    public virtual Institution Institution { get; set; } = null!;

}
