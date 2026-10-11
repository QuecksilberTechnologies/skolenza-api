using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class PayrollStaffMemberDetail
{
    public long Id { get; set; }

    public long PayrollStaffMemberId { get; set; }

    public long ComponentId { get; set; }

    public decimal Amount { get; set; }

    public int ComponentType { get; set; }

    public bool IsActive { get; set; }

    public long? AddedById { get; set; }

    public DateTime? AddedDateTime { get; set; }

    public virtual SalaryComponentMaster Component { get; set; } = null!;

    public virtual PayrollStaffMember PayrollStaffMember { get; set; } = null!;
}
