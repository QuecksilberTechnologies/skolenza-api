using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class StaffEmploymentType
{
    public int Id { get; set; }

    /// <summary>Null only for retained legacy catalogue rows; institution APIs never expose them.</summary>
    public long? InstitutionId { get; set; }

    public string? TypeName { get; set; }

    public string? Description { get; set; }

    public string? Remark { get; set; }

    public bool? IsActive { get; set; }

    public long? AddedById { get; set; }

    public DateTime? AddedDateTime { get; set; }

    public long? UpdatedById { get; set; }

    public DateTime? UpdatedDateTime { get; set; }

    public bool? IsSoftDeleted { get; set; }

    public long? SoftDeletedById { get; set; }

    public DateTime? SoftDeletedDateTime { get; set; }


    public virtual ICollection<StaffMember> StaffMember { get; set; } = new List<StaffMember>();

    public virtual ICollection<StaffEmploymentTypeBasicMenu> StaffEmploymentTypeBasicMenu { get; set; } = new List<StaffEmploymentTypeBasicMenu>();

    public virtual ICollection<StaffMembersChangedTypeHistory> StaffMembersChangedTypeHistoryNewStaffEmploymentType { get; set; } = new List<StaffMembersChangedTypeHistory>();

    public virtual ICollection<StaffMembersChangedTypeHistory> StaffMembersChangedTypeHistoryOldStaffEmploymentType { get; set; } = new List<StaffMembersChangedTypeHistory>();

}
