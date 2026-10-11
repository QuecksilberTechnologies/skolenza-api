using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class AssetImage
{
    public long Id { get; set; }

    public long InstitutionId { get; set; }

    public long AssetId { get; set; }

    public int AssetImageType { get; set; }

    public string? AssetImagePath { get; set; }

    public string? Remark { get; set; }

    public bool IsActive { get; set; }

    public bool IsSoftDeleted { get; set; }

    public long? AddedById { get; set; }

    public DateTime AddedDateTime { get; set; }

    public long? UpdatedById { get; set; }

    public DateTime? UpdatedDateTime { get; set; }

    public long? SoftDeletedById { get; set; }

    public DateTime? DeletedDateTime { get; set; }

    public bool IsPrimary { get; set; }

    public virtual Asset Asset { get; set; } = null!;
}
