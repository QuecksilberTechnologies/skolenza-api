using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class BasicMenu
{
    public int Id { get; set; }

    public string MenuName { get; set; } = null!;

    public string? MenuUrl { get; set; }

    public int? ParentMenuId { get; set; }

    public string Remark { get; set; } = null!;

    public bool IsActive { get; set; }

    public string? ImageIcon { get; set; }

    public virtual ICollection<StaffEmploymentTypeBasicMenu> StaffEmploymentTypeBasicMenu { get; set; } = new List<StaffEmploymentTypeBasicMenu>();

    public virtual ICollection<BasicMenu> InverseParentMenu { get; set; } = new List<BasicMenu>();

    public virtual BasicMenu? ParentMenu { get; set; }
}
