using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class StaffMemberDependent
{
    public long Id { get; set; }

    public long? StaffMemberId { get; set; }

    public string? DependentName { get; set; }

    public int? Relation { get; set; }

    public DateTime? DateOfBirth { get; set; }

    public bool? IsCoveredInPolicy { get; set; }

    public bool? IsMarried { get; set; }

    public string? Remark { get; set; }

    public string? Description { get; set; }

    public DateTime? UpdatedDateTime { get; set; }

    public long? AddedById { get; set; }

    public DateTime? AddedDateTime { get; set; }

    public long? UpdatedById { get; set; }

    public long? SoftDeletedById { get; set; }

    public DateTime? DeletedDateTime { get; set; }

    public bool? IsSoftDeleted { get; set; }

    public long? InfoVerifiedById { get; set; }

    public bool? IsInfoVerified { get; set; }

    public DateTime? InfoVerifiedDateTime { get; set; }

    public bool? IsEditAllowed { get; set; }

    public bool? IsActive { get; set; }

    public string? FilePath { get; set; }

    public string? FileName { get; set; }

    public bool? HasProofUploaded { get; set; }

    public int? FileType { get; set; }

    public virtual StaffMember? StaffMember { get; set; }

}
