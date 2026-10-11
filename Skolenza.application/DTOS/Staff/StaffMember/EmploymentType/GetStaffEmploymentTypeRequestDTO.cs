using skolenza.application.DTOs.BaseDTO;

namespace skolenza.application.DTOS.Staff.Type;

/// <summary>Institution scope is resolved from authentication, never from a query parameter.</summary>
public sealed class GetStaffEmploymentTypeRequestDTO : PermissionRequestDTO
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
