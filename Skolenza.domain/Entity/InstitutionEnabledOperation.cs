using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class InstitutionEnabledOperation
{
    public long Id { get; set; }

    public long InstitutionId { get; set; }

    public int ModuleId { get; set; }

    public int OperationId { get; set; }

    public bool? IsOperationUsed { get; set; }

    public bool IsEnabled { get; set; }

    public long? AddedById { get; set; }

    public DateTime? AddedDateTime { get; set; }

    public long? UpdatedById { get; set; }

    public DateTime? UpdatedDateTime { get; set; }

    public virtual Module Module { get; set; } = null!;

    public virtual Operation Operation { get; set; } = null!;

    public virtual Institution Institution { get; set; } = null!;
}
