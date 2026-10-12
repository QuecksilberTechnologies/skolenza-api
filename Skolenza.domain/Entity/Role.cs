using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class Role
{
    public int Id { get; set; }

    public long? InstitutionId { get; set; }

    public string? RoleName { get; set; }

    public int RoleType { get; set; }

    public string? Remark { get; set; }

    public bool IsActive { get; set; }

    public bool? IsSystemDefault { get; set; }

    public bool? IsSoftDeleted { get; set; }

    public long AddedById { get; set; }

    public DateTime AddedDateTime { get; set; }

    public long? UpdatedById { get; set; }

    public DateTime? UpdatedDateTime { get; set; }

    public long? SoftDeletedById { get; set; }

    public DateTime? DeletedDateTime { get; set; }

    public virtual ICollection<RoleModuleAndPermission> RoleModuleAndPermission { get; set; } = new List<RoleModuleAndPermission>();

    public virtual Institution? Institution { get; set; }


    public virtual ICollection<Ticket> Ticket { get; set; } = new List<Ticket>();


    public virtual ICollection<UserRole> UserRole { get; set; } = new List<UserRole>();
}






