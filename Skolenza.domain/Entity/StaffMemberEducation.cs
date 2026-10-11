using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class StaffMemberEducation
{
    public int Id { get; set; }

    public long StaffMemberId { get; set; }

    public string? Degree { get; set; }

    public string? InstituteName { get; set; }

    public string? Remark { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string? ScoreValue { get; set; }

    public string? GradeDivision { get; set; }

    public bool? EducationGap { get; set; }

    public string? ReasonOfEducationGap { get; set; }

    public long AddedById { get; set; }

    public DateTime AddedDateTime { get; set; }

    public long? UpdatedById { get; set; }

    public DateTime? UpdatedDateTime { get; set; }

    public long? SoftDeletedById { get; set; }

    public DateTime? DeletedDateTime { get; set; }

    public bool? IsSoftDeleted { get; set; }

    public long? InfoVerifiedById { get; set; }

    public bool? IsInfoVerified { get; set; }

    public DateTime? InfoVerifiedDateTime { get; set; }

    public bool? IsEditAllowed { get; set; }

    public bool? IsActive { get; set; }

    public string? FilePath { get; set; }

    public int? FileType { get; set; }

    public string? FileName { get; set; }

    public bool HasEducationDocUploded { get; set; } = false;

    public int? ScoreType { get; set; }

    public double? GapYears { get; set; }
}
