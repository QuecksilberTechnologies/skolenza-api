using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class PayrollStaffMember
{
    public long Id { get; set; }

    public long PayrollRunId { get; set; }

    public long StaffMemberId { get; set; }

    public decimal? PaidDays { get; set; }

    public decimal? LOPDays { get; set; }

    public decimal? GrossSalary { get; set; }

    public decimal? TotalEarnings { get; set; }

    public decimal? TotalDeductions { get; set; }

    public decimal? TaxAmount { get; set; }

    public decimal? NetSalary { get; set; }

    public string? CurrencyCode { get; set; }

    public bool IsActive { get; set; }

    public bool? IsSoftDeleted { get; set; }

    public long? AddedById { get; set; }

    public DateTime? AddedDateTime { get; set; }

    public virtual StaffMember StaffMember { get; set; } = null!;

    public virtual ICollection<PayrollStaffMemberDetail> PayrollStaffMemberDetail { get; set; } = new List<PayrollStaffMemberDetail>();

    public virtual PayrollRun PayrollRun { get; set; } = null!;
}
