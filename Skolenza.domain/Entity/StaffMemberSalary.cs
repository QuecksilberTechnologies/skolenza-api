using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class StaffMemberSalary
{
    public long Id { get; set; }

    public long StaffMemberId { get; set; }

    public long SalaryStructureId { get; set; }

    public decimal CTC { get; set; }

    public decimal? BasicOverride { get; set; }

    public decimal? HRAOverride { get; set; }

    public string? CurrencyCode { get; set; }

    public DateOnly EffectiveFrom { get; set; }

    public DateOnly? EffectiveTo { get; set; }

    public bool IsActive { get; set; }

    public bool? IsSoftDeleted { get; set; }

    public long? AddedById { get; set; }

    public DateTime? AddedDateTime { get; set; }

    public long? UpdatedById { get; set; }

    public DateTime? UpdatedDateTime { get; set; }

    public long? SoftDeletedById { get; set; }

    public DateTime? DeletedDateTime { get; set; }

    public virtual StaffMember StaffMember { get; set; } = null!;

    public virtual SalaryStructure SalaryStructure { get; set; } = null!;
}
