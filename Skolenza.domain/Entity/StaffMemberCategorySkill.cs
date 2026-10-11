using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class StaffMemberCategorySkill
{
    public long Id { get; set; }

    public long StaffMemberId { get; set; }

    public int CategoryId { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public virtual Category Category { get; set; } = null!;

    public virtual StaffMember StaffMember { get; set; } = null!;
}
