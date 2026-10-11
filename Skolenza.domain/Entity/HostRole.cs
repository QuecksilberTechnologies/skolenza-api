using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class HostRole
{
    public long Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public long? AddedById { get; set; }

    public DateTime AddedDateTime { get; set; }

    public long? UpdatedById { get; set; }

    public DateTime? UpdatedDateTime { get; set; }

    public long? DeletedById { get; set; }

    public DateTime? DeletedDateTime { get; set; }

    public bool IsSoftDeleted { get; set; }

    public virtual ICollection<HostRoleModuleAndPermission> HostRoleModuleAndPermission { get; set; } = new List<HostRoleModuleAndPermission>();

    public virtual ICollection<HostUser> HostUser { get; set; } = new List<HostUser>();
}
