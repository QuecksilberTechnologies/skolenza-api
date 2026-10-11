using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class AssetStatus
{
    public int Id { get; set; }

    public long InstitutionId { get; set; }

    public string StatusName { get; set; } = null!;

    public string? Description { get; set; }

    public bool? IsActive { get; set; }

    public bool? IsSoftDeleted { get; set; }

    public long AddedById { get; set; }

    public DateTime AddedDateTime { get; set; }

    public long? UpdatedById { get; set; }

    public DateTime? UpdatedDateTime { get; set; }

    public long? SoftDeletedById { get; set; }

    public DateTime? DeletedDateTime { get; set; }

    public string? ColorKey { get; set; }

    public virtual ICollection<Asset> Asset { get; set; } = new List<Asset>();
}
