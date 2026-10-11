using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class RoleModuleAndPermission
{
    public int Id { get; set; }

    public int? RoleId { get; set; }

    public int? ModuleId { get; set; }

    public int? OperationId { get; set; }

    public bool? HasAccess { get; set; }

    public bool? IsActive { get; set; }

    public string? Remark { get; set; }

    public bool? IsOperational { get; set; }

    public string? ImageIcon { get; set; }

    public long? AddedById { get; set; }

    public DateTime? AddedDateTime { get; set; }

    public long? UpdatedById { get; set; }

    public DateTime? UpdateDateTime { get; set; }

    public long? SoftDeletedById { get; set; }

    public DateTime? DeletedDateTime { get; set; }

    public bool IsSoftDeleted { get; set; }

    public DateTime? UpdatedDateTime { get; set; }

    public virtual Module? Module { get; set; }

    public virtual Operation? Operation { get; set; }

    public virtual Role? Role { get; set; }
}
