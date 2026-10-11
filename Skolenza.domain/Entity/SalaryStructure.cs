using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class SalaryStructure
{
    public long Id { get; set; }

    public long InstitutionId { get; set; }

    public string StructureName { get; set; } = null!;

    public string? Description { get; set; }

    public bool? IsDefault { get; set; }

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

    public virtual ICollection<StaffMemberSalary> StaffMemberSalary { get; set; } = new List<StaffMemberSalary>();

    public virtual ICollection<SalaryStructureDetail> SalaryStructureDetail { get; set; } = new List<SalaryStructureDetail>();

    public virtual Institution Institution { get; set; } = null!;
}
