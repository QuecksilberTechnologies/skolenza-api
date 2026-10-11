using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class PolicyType
{
    public int Id { get; set; }

    public long? InstitutionId { get; set; }

    public string PolicyName { get; set; } = null!;

    public string? Description { get; set; }

    public bool? IsActive { get; set; }

    public bool? IsSoftDelete { get; set; }

    public long? AddedById { get; set; }

    public DateTime? AddedDateTime { get; set; }

    public long? UpdateById { get; set; }

    public DateTime? UpdateDateTime { get; set; }

    public long? SoftDeleteById { get; set; }

    public DateTime? SoftDeleteDateTime { get; set; }

    public bool IsStructured { get; set; } = false; 

    public int PolicyTypeEnumVal { get; set; }

    public bool HasPolicyDocUploaded { get; set; }

    public int? PolicyCategoryId { get; set; }

    public string? PolicyTypeCode { get; set; }

    public string? DefaultCurrencyCode { get; set; }


    public virtual Institution? Institution { get; set; }

}
