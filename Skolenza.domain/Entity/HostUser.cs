using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class HostUser
{
    public long Id { get; set; }

    public long HostRoleId { get; set; }

    public string Name { get; set; } = null!;

    public string LoginId { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? Email { get; set; }

    public string? MobileNumber { get; set; }

    public bool IsActive { get; set; }

    public bool IsSoftDeleted { get; set; }

    public long? AddedById { get; set; }

    public DateTime AddedDateTime { get; set; }

    public long? UpdatedById { get; set; }

    public DateTime? UpdatedDateTime { get; set; }

    public long? DeletedById { get; set; }

    public DateTime? DeletedDateTime { get; set; }

    public virtual HostRole HostRole { get; set; } = null!;

    public virtual ICollection<RefreshToken> RefreshToken { get; set; } = new List<RefreshToken>();
}
