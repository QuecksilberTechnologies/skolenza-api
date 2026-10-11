using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class StaffMemberWorkHistory
{
    public long Id { get; set; }

    public long StaffMemberWorkProfileId { get; set; }

    public string CompanyName { get; set; } = null!;

    public string Designation { get; set; } = null!;

    public decimal? Ctc { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string? ReasonForLeaving { get; set; }

    public long? WorkingCountryId { get; set; }

    public long? WorkingStateId { get; set; }

    public long? WorkingDistrictId { get; set; }

    public bool IsWfh { get; set; }

    public bool IsForeignExperience { get; set; }

    public string? ReportingManagerName { get; set; }

    public string? ReportingManagerNumber { get; set; }

    public string? VerificationEmail { get; set; }

    public bool IsVerified { get; set; }

    public long? VerifiedById { get; set; }

    public DateTime? VerifiedDateTime { get; set; }

    public string? VerificationMode { get; set; }

    public bool IsActive { get; set; }

    public bool IsEditAllowed { get; set; }

    public long? AddedById { get; set; }

    public DateTime AddedDateTime { get; set; }

    public long? UpdatedById { get; set; }

    public DateTime? UpdatedDateTime { get; set; }

    public long? SoftDeletedById { get; set; }

    public DateTime? DeletedDateTime { get; set; }

    public virtual ICollection<StaffMemberWorkDocument> StaffMemberWorkDocument { get; set; } = new List<StaffMemberWorkDocument>();

    public virtual StaffMemberWorkProfile StaffMemberWorkProfile { get; set; } = null!;
}
