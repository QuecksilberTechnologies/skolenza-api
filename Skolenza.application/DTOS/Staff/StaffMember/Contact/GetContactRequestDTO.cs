// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the request model for retrieving Contact.
// ================================================================

using skolenza.application.DTOS.Common;
using skolenza.application.DTOS.Pagination;

namespace skolenza.application.DTOS.Staff.Contact
{
    /// <summary>
    /// Represents the GetContactRequestDTO data transfer model.
    /// </summary>
    public class GetContactRequestDTO : PermissionPagedRequestDTO
    {
          
        public required string UserStaffMemberId { get; set; }
        public required string StaffMemberId { get; set; }      
    }
}
