using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class StaffMemberImage
{
    public long Id { get; set; }

    public long? InstitutionId { get; set; }

    public string? FilePath { get; set; }

    public long StaffMemberId { get; set; }

    public bool IsActive { get; set; }

    public bool IsPrimary { get; set; }

    public bool IsSoftDeleted { get; set; }

    public long? SoftDeletedById { get; set; }

    public DateTime? DeletedDateTime { get; set; }

    public int? FileType { get; set; }

    public long? AddedById { get; set; }

    public DateTime? AddedDateTime { get; set; }

    public long? UpdateById { get; set; }

    public DateTime? UpdatedDateTime { get; set; }

    public bool HasImageUploaded { get; set; } = false;

    public string? FileName { get; set; }

    public virtual StaffMember? StaffMember { get; set; }
}
