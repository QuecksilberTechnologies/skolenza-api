using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class AssetAssignment
{
    public long Id { get; set; }

    public long InstitutionId { get; set; }

    public long RequestId { get; set; }

    public long StaffMemberId { get; set; }

    public long? AssetId { get; set; }

    public DateTime? AssignedDate { get; set; }

    public DateTime? ExpectedReturnDate { get; set; }

    public DateTime? ActualReturnDate { get; set; }

    public int? AssignmentStatus { get; set; }

    public string? AssetConditionAtAssign { get; set; }

    public string? AssetConditionAtReturn { get; set; }

    public bool? IsActive { get; set; }

    public bool? IsSoftDeleted { get; set; }

    public long? AddedById { get; set; }

    public DateTime? AddedDateTime { get; set; }

    public long? UpdatedById { get; set; }

    public DateTime? UpdatedByDateTime { get; set; }

    public long? SoftDeletedById { get; set; }

    public DateTime? SoftDeletedByDateTime { get; set; }

    public virtual Asset? Asset { get; set; }

    public virtual AssetRequest Request { get; set; } = null!;
}
