using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class StaffMemberWorkDocument
{
    public long Id { get; set; }

    public long StaffMemberWorkHistoryId { get; set; }

    public int WorkDocumentTypeId { get; set; }

    public string FileName { get; set; } = null!;

    public string FilePath { get; set; } = null!;

    public bool IsVerified { get; set; }

    public long? VerifiedById { get; set; }

    public DateTime? VerifiedDateTime { get; set; }

    public bool IsActive { get; set; }

    public long? AddedById { get; set; }

    public DateTime AddedDateTime { get; set; }

    public virtual StaffMemberWorkHistory StaffMemberWorkHistory { get; set; } = null!;

    public virtual WorkDocumentType WorkDocumentType { get; set; } = null!;
}
