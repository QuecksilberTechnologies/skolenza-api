using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class StaffMemberCodePattern
{
    public long Id { get; set; }

    public long InstitutionId { get; set; }

    public string? Prefix { get; set; }

    public bool IncludeYear { get; set; }

    public bool IncludeMonth { get; set; }

    public bool IncludeDepartment { get; set; }

    public string Separator { get; set; } = null!;

    public int RunningNumberLength { get; set; }

    public int LastUsedNumber { get; set; }

    public bool IsActive { get; set; }

    public long AddedById { get; set; }

    public DateTime AddedDateTime { get; set; }

    public long? UpdatedById { get; set; }

    public DateTime? UpdatedDateTime { get; set; }

    public virtual Institution Institution { get; set; } = null!;
}
