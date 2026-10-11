using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class SalaryComponentMaster
{
    public long Id { get; set; }

    public long InstitutionId { get; set; }

    public int? CountryId { get; set; }

    public int? StateId { get; set; }

    public string ComponentName { get; set; } = null!;

    public int ComponentType { get; set; }

    public int CalculationType { get; set; }

    public string? FormulaExpression { get; set; }

    public long? DependsOnComponentId { get; set; }

    public bool IsTaxable { get; set; }

    public int? ComplianceTypeId { get; set; }

    public bool IsStatutory { get; set; }

    public bool? IsProrated { get; set; }

    public bool? IsVisibleInPayslip { get; set; }

    public bool? IsEditable { get; set; }

    public int? DisplayOrder { get; set; }

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

    public virtual ComplianceTypeMaster? ComplianceType { get; set; }

    public virtual Country? Country { get; set; }

    public virtual SalaryComponentMaster? DependsOnComponent { get; set; }

    public virtual ICollection<SalaryComponentMaster> InverseDependsOnComponent { get; set; } = new List<SalaryComponentMaster>();

    public virtual ICollection<PayrollStaffMemberDetail> PayrollStaffMemberDetail { get; set; } = new List<PayrollStaffMemberDetail>();

    public virtual ICollection<SalaryStructureDetail> SalaryStructureDetailComponent { get; set; } = new List<SalaryStructureDetail>();

    public virtual ICollection<SalaryStructureDetail> SalaryStructureDetailDependsOnComponent { get; set; } = new List<SalaryStructureDetail>();

    public virtual State? State { get; set; }

    public virtual Institution Institution { get; set; } = null!;
}
