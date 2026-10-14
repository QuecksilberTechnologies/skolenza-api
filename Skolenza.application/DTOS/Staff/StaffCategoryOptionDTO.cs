namespace skolenza.application.DTOS.Staff;

/// <summary>One active platform Staff Category option.</summary>
public sealed record StaffCategoryOptionDTO(
    short Id,
    string Code,
    string Label,
    string? Description);

/// <summary>Host-managed Staff Category catalogue row.</summary>
public sealed record HostStaffCategoryResponseDTO(
    short Id,
    string Code,
    string Name,
    string? Description,
    short DisplayOrder,
    bool IsActive,
    bool IsSystemDefined);

/// <summary>Creates or updates one platform Staff Category.</summary>
public sealed class SaveHostStaffCategoryRequestDTO : DTOs.BaseDTO.PermissionRequestDTO
{
    public short? Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public short DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
