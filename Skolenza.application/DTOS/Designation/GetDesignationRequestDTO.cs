// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines client-supplied filters for designation listings.
// ================================================================

using skolenza.application.DTOS.Pagination;

namespace skolenza.application.DTOs.Designation
{
    /// <summary>
    /// Represents filters and paging inputs for designation projections.
    /// </summary>
    public class GetDesignationRequestDTO : PermissionPagedRequestDTO
    {
        public int DepartmentId { get; set; }
        public string? DesignationName { get; set; }
        public bool? IsActive { get; set; }
    }
}
