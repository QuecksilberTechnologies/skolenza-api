using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class StaffMemberIdentity
{
    public long Id { get; set; }

    public long StaffMemberId { get; set; }

    public int IdentityCategoryDocumentId { get; set; }

    public string IdentityValue { get; set; } = null!;

    public string? DocumentFileName { get; set; }

    public string? DocumentFilePath { get; set; }

    public bool IsInfoVerified { get; set; }

    public long? InfoVerifiedById { get; set; }

    public DateTime? InfoVerifiedDateTime { get; set; }

    public bool IsEditAllowed { get; set; }

    public bool HasIdentityUploaded { get; set; }

    public DateOnly EffectiveFrom { get; set; }

    public DateOnly? EffectiveTo { get; set; }

    public bool IsActive { get; set; }

    public bool IsSoftDeleted { get; set; }

    public long? AddedById { get; set; }

    public DateTime AddedDateTime { get; set; }

    public long? UpdatedById { get; set; }

    public DateTime? UpdatedDateTime { get; set; }

    public long? SoftDeletedById { get; set; }

    public DateTime? DeletedDateTime { get; set; }

    public virtual StaffMember StaffMember { get; set; } = null!;

    public virtual IdentityCategoryDocument IdentityCategoryDocument { get; set; } = null!;
}
