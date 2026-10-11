using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class StaffMemberContact
{
    public long Id { get; set; }

    public long StaffMemberId { get; set; }

    public int? ContactType { get; set; }

    public int? Relation { get; set; }

    public string? ContactName { get; set; }

    public string? ContactNumber { get; set; }

    public string? AlternateNumber { get; set; }

    public string? Email { get; set; }

    public bool IsPrimary { get; set; } =false;

    public int? CountryId { get; set; }

    public int? StateId { get; set; }

    public int? DistrictId { get; set; }

    public int? LocalityId { get; set; }

    public string? HouseNo { get; set; }

    public string? LandMark { get; set; }

    public string? Street { get; set; }

    public string? Address { get; set; }

    public string? Remark { get; set; }

    public bool? IsActive { get; set; }

    public bool? IsSoftDeleted { get; set; }

    public long AddedById { get; set; }

    public DateTime AddedDateTime { get; set; }

    public long? UpdatedById { get; set; }

    public DateTime? UpdatedDateTime { get; set; }

    public long? SoftDeletedById { get; set; }

    public DateTime? DeletedDateTime { get; set; }

    public bool? IsEditAllowed { get; set; }

    public bool? IsInfoVerified { get; set; }

    public long? InfoVerifiedById { get; set; }

    public DateTime? InfoVerifiedDateTime { get; set; }

    public string? Description { get; set; }

    public virtual StaffMember StaffMember { get; set; } = null!;

    public virtual Locality? Locality { get; set; }
}
