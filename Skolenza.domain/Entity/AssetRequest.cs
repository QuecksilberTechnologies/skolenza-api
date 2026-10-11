using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class AssetRequest
{
    public long Id { get; set; }

    public long InstitutionId { get; set; }

    public long StaffMemberId { get; set; }

    public int AssetTypeId { get; set; }

    public DateTime? RequestDate { get; set; }

    public int RequestStatus { get; set; }

    public bool? IsActive { get; set; }

    public bool? IsSoftDeleted { get; set; }

    public long? AddedById { get; set; }

    public DateTime? AddedDateTime { get; set; }

    public long? UpdatedById { get; set; }

    public DateTime? UpdatedByDateTime { get; set; }

    public long? SoftDeletedById { get; set; }

    public DateTime? SoftDeletedByDateTime { get; set; }

    public virtual ICollection<AssetAssignment> AssetAssignment { get; set; } = new List<AssetAssignment>();
}
