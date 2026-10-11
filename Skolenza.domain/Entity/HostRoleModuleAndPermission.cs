using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class HostRoleModuleAndPermission
{
    public long Id { get; set; }

    public long HostRoleId { get; set; }

    public int ModuleId { get; set; }

    public int OperationId { get; set; }

    public bool IsActive { get; set; }

    public bool IsSoftDeleted { get; set; }

    public long? AddedById { get; set; }

    public DateTime AddedDateTime { get; set; }

    public long? UpdatedById { get; set; }

    public DateTime? UpdatedDateTime { get; set; }

    public long? DeletedById { get; set; }

    public DateTime? DeletedDateTime { get; set; }

    public virtual HostRole HostRole { get; set; } = null!;

    public virtual Module Module { get; set; } = null!;

    public virtual Operation Operation { get; set; } = null!;
}
