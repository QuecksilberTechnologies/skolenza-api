using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class DataViewStructure
{
    public int Id { get; set; }

    public string? DisplayOn { get; set; }

    public string? Discription { get; set; }

    public string? Remark { get; set; }

    public bool IsDisplayedAtPriority { get; set; }

    public virtual ICollection<ModuleOperationMapping> ModuleOperationMapping { get; set; } = new List<ModuleOperationMapping>();
}
