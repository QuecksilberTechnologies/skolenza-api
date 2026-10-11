using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class InstitutionProfile
{
    public long Id { get; set; }

    public long InstitutionId { get; set; }

    public string? LogoUrl { get; set; }

    public string? ThemeColor { get; set; }

    public string? BusinessType { get; set; }

    public string? Industry { get; set; }

    public int? TotalStaffMembers { get; set; }

    public int? TotalCampuses { get; set; }

    public int? FoundedYear { get; set; }

    public string? WebsiteUrl { get; set; }

    public virtual Institution Institution { get; set; } = null!;
}
