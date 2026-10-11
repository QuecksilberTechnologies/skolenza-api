using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class Module
{
    public int Id { get; set; }

    public long? InstitutionId { get; set; }

    public string? ModuleCode { get; set; }

    public string ModuleName { get; set; } = null!;

    /// <summary>
    /// Stable client page identity. Unlike <see cref="Urlpath"/>, this value is
    /// immutable after the Module is created and identifies a registered frontend page component.
    /// </summary>
    public string? PageName { get; set; }

    public string? DisplayName { get; set; }

    public string? Urlpath { get; set; }

    public int? ParentModuleId { get; set; }

    public bool? IsLeafNode { get; set; }

    public bool IsModuleDisplayInUI { get; set; }

    public bool IsCommonMenu { get; set; }
    public short ModuleScope { get; set; }

    public bool IsActive { get; set; }

    public string? ImageIconWeb { get; set; }

    public string? ImageIconMobile { get; set; }

    public int? ItemPriority { get; set; }

    public string? Remark { get; set; }

    public long? AddedById { get; set; }

    public DateTime? AddedDateTime { get; set; }

    public long? UpdatedById { get; set; }

    public DateTime? UpdatedDateTime { get; set; }

    public virtual ICollection<Module> InverseParentModule { get; set; } = new List<Module>();

    public virtual ICollection<ModuleOperationMapping> ModuleOperationMapping { get; set; } = new List<ModuleOperationMapping>();
    public virtual ICollection<HostRoleModuleAndPermission> HostRoleModuleAndPermission { get; set; } = new List<HostRoleModuleAndPermission>();

    public virtual Module? ParentModule { get; set; }

    public virtual ICollection<PlanModuleMapping> PlanModuleMapping { get; set; } = new List<PlanModuleMapping>();

    public virtual ICollection<RoleModuleAndPermission> RoleModuleAndPermission { get; set; } = new List<RoleModuleAndPermission>();

    public virtual ICollection<InstitutionEnabledModule> InstitutionEnabledModule { get; set; } = new List<InstitutionEnabledModule>();

    public virtual ICollection<InstitutionEnabledOperation> InstitutionEnabledOperation { get; set; } = new List<InstitutionEnabledOperation>();
}
