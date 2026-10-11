using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class StaffMemberStatutoryAccount
{
    public long Id { get; set; }

    public long StaffMemberId { get; set; }

    public int StatutoryTypeId { get; set; }

    public string AccountNumber { get; set; } = null!;

    public string? EmployerCode { get; set; }

    public DateOnly ContributionStartDate { get; set; }

    public DateOnly? ContributionEndDate { get; set; }

    public bool IsActive { get; set; }

    public long? AddedById { get; set; }

    public long? UpdatedById { get; set; }

    public long? DeletedById { get; set; }

    public DateTime AddedDateTime { get; set; }

    public DateTime? UpdatedDateTime { get; set; }

    public DateTime? DeletedDateTime { get; set; }

    public virtual StatutoryType StatutoryType { get; set; } = null!;
}
