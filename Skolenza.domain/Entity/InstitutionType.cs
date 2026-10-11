using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class InstitutionType
{
    public int Id { get; set; }

    public string InstitutionTypeName { get; set; } = null!;

    public string? Description { get; set; }

    public string? Remark { get; set; }

    public bool IsActive { get; set; }

    public long? AddedById { get; set; }

    public DateTime? AddedDateTime { get; set; }

    public long? UpdatedById { get; set; }

    public DateTime? UpdatedDateTime { get; set; }

    public bool? IsSoftDeted { get; set; }

    public long? SoftDeletedById { get; set; }

    public DateTime? SoftDeletedDateTime { get; set; }

    public virtual ICollection<Institution> Institutions { get; set; } = new List<Institution>();
}
