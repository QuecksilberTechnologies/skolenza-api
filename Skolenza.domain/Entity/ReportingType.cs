using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class ReportingType
{
    public int Id { get; set; }

    public string TypeName { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsActive { get; set; }
    public bool IsSoftDeleted{ get; set; } = false;

    public long? SoftDeletedById { get; set; }
    public long? AddedById { get; set; }

    public DateTime? AddedDateTime { get; set; }
    public DateTime? SoftDeletedDateTime { get; set; }

    public long? UpdatedById { get; set; }
    public long InstitutionId { get; set; }

    public DateTime? UpdatedDateTime { get; set; }

    public virtual ICollection<StaffMemberManagerMapping> StaffMemberManagerMapping { get; set; } = new List<StaffMemberManagerMapping>();
}
