using System.ComponentModel.DataAnnotations;
using skolenza.application.DTOs.BaseDTO;

namespace skolenza.application.DTOS.Staff.Type;

/// <summary>Institution-owned type creation; ownership and audit fields come from the login context.</summary>
public class CreateStaffEmploymentTypeDTO : PermissionRequestDTO
{
    [Required, StringLength(255)]
    public string TypeName { get; set; } = string.Empty;
    [StringLength(255)]
    public string? Description { get; set; }
    [StringLength(255)]
    public string? Remark { get; set; }
    public bool IsActive { get; set; } = true;
}
