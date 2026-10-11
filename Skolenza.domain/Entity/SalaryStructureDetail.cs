using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class SalaryStructureDetail
{
    public long Id { get; set; }

    public long SalaryStructureId { get; set; }

    public long ComponentId { get; set; }

    public int CalculationType { get; set; }

    public decimal? Value { get; set; }

    public string? FormulaExpression { get; set; }

    public long? DependsOnComponentId { get; set; }

    public int CalculationOrder { get; set; }

    public decimal? MinAmount { get; set; }

    public decimal? MaxAmount { get; set; }

    public bool IsActive { get; set; }

    public bool? IsSoftDeleted { get; set; }

    public long? AddedById { get; set; }

    public DateTime? AddedDateTime { get; set; }

    public long? UpdatedById { get; set; }

    public DateTime? UpdatedDateTime { get; set; }

    public long? SoftDeletedById { get; set; }

    public DateTime? DeletedDateTime { get; set; }

    public virtual SalaryComponentMaster Component { get; set; } = null!;

    public virtual SalaryComponentMaster? DependsOnComponent { get; set; }

    public virtual SalaryStructure SalaryStructure { get; set; } = null!;
}
