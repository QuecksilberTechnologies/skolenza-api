using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class Operation
{
    public int Id { get; set; }

    public string OperationName { get; set; } = null!;

    public string? Remark { get; set; }

    public int? OperationType { get; set; }

    public bool IsActive { get; set; }

    public long? AddedById { get; set; }

    public DateTime AddedDateTime { get; set; }

    public long? UpdatedById { get; set; }

    public DateTime? UpdatedDateTime { get; set; }

    public string? IconImage { get; set; }

    public virtual ICollection<ModuleOperationMapping> ModuleOperationMapping { get; set; } = new List<ModuleOperationMapping>();
    public virtual ICollection<HostRoleModuleAndPermission> HostRoleModuleAndPermission { get; set; } = new List<HostRoleModuleAndPermission>();

    public virtual ICollection<RoleModuleAndPermission> RoleModuleAndPermission { get; set; } = new List<RoleModuleAndPermission>();

    public virtual ICollection<InstitutionEnabledOperation> InstitutionEnabledOperation { get; set; } = new List<InstitutionEnabledOperation>();
}
