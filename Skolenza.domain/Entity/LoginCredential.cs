using System;
using System.Collections.Generic;

// ============================================================================
// Author      : Deepesh Gupta
// Company     : Quecksilber Technologies
// Role        : CEO
// Purpose     : Represents Institution StaffMember credentials independently of common refresh-token ownership.
// ============================================================================

namespace skolenza.domain.Entity;

public partial class LoginCredential
{
    public long Id { get; set; }

    public long? InstitutionId { get; set; }

    public long StaffMemberId { get; set; }

    public string LoginId { get; set; } = null!;

    public string? Password { get; set; }

    public bool HasFirstLogin { get; set; }

    public bool? IsPasswordChangeRequired { get; set; }

    public string? MacAddress { get; set; }

    public bool IsActive { get; set; }

    public string? Remark { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public int? LoginDevice { get; set; }

    public string? IpAddressLocal { get; set; }

    public string? IpAddressPublic { get; set; }

    public bool? IsSoftDeleted { get; set; }

    public long? AddedById { get; set; }

    public DateTime? AddedDateTime { get; set; }

    public long? UpdatedById { get; set; }

    public DateTime? UpdatedDateTime { get; set; }

    public long? SoftDeletedById { get; set; }

    public DateTime? DeletedDateTime { get; set; }

    public bool IsOnboard { get; set; }

    public virtual StaffMember StaffMember { get; set; } = null!;
    public virtual ICollection<RefreshToken> RefreshToken { get; set; } = new List<RefreshToken>();

}
