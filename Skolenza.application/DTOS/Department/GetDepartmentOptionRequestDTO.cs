// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the authorized request model for department options.
// ================================================================

using skolenza.application.DTOs.BaseDTO;

namespace skolenza.application.DTOS.Department;

/// <summary>
/// Carries the Department module-operation context for option lookups while
/// retaining the legacy optional request values.
/// </summary>
public sealed class GetDepartmentOptionRequestDTO : PermissionRequestDTO
{
    public string? UserStaffMemberId { get; set; }

    public DateTime? TodaysDate { get; set; }
}
