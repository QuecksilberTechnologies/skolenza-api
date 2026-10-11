using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class Gender
{
    public int Id { get; set; }

    public string GenderName { get; set; } = null!;

    public virtual ICollection<StaffMember> StaffMember { get; set; } = new List<StaffMember>();


    public virtual ICollection<Institution> Institution { get; set; } = new List<Institution>();
}
