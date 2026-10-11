using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class WorkDocumentType
{
    public int Id { get; set; }

    public string DocumentTypeName { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual ICollection<StaffMemberWorkDocument> StaffMemberWorkDocument { get; set; } = new List<StaffMemberWorkDocument>();
}
