using skolenza.application.DTOS.Common;

namespace skolenza.application.DTOS.Host;

/// <summary>Host upload. InstitutionId is required only for card inventory and applies to every row.</summary>
public sealed class HostBulkImportPreviewRequestDTO : BulkImportPreviewRequestDTO
{
    public string? InstitutionId { get; set; }
}

/// <summary>Host job action. Supply the same selected InstitutionId for card jobs.</summary>
public sealed class HostBulkImportJobRequestDTO : BulkImportJobRequestDTO
{
    public string? InstitutionId { get; set; }
}

#region Host catalogue import rows

public class HostModuleImportRowDTO
{
    public string ModuleCode { get; set; } = string.Empty;
    public string ModuleName { get; set; } = string.Empty;
    public string PageName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? URLPath { get; set; }
    public bool IsModuleDisplayInUI { get; set; }
    public bool IsCommonMenu { get; set; }
    public short ModuleScope { get; set; }
    public bool IsActive { get; set; } = true;
    public string? ImageIconWeb { get; set; }
    public string? ImageIconMobile { get; set; }
    public int? ItemPriority { get; set; }
    public string? Remark { get; set; }
}

public sealed class HostSubModuleImportRowDTO : HostModuleImportRowDTO
{
    public string ParentModuleCode { get; set; } = string.Empty;
}

public sealed class HostOperationImportRowDTO
{
    public string OperationName { get; set; } = string.Empty;
    public int OperationType { get; set; }
    public string? Remark { get; set; }
    public bool IsActive { get; set; } = true;
    public string? IconImage { get; set; }
}

public sealed class HostModuleOperationImportRowDTO
{
    public string ModuleCode { get; set; } = string.Empty;
    public int OperationType { get; set; }
    public int? DataViewStructureId { get; set; }
    public int? PageTypeId { get; set; }
    public string? PageURL { get; set; }
    public string? IconURL { get; set; }
    public bool? IsCommonItem { get; set; }
    public bool? IsOperational { get; set; }
    public int? Priority { get; set; }
    public string? Remark { get; set; }
    public bool? IsActive { get; set; }
}

#endregion
