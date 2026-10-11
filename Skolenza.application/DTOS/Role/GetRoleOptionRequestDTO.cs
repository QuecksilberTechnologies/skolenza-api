// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines filtering criteria for institution role option queries.
// ================================================================
using skolenza.application.DTOs.BaseDTO;

    /// <summary>
    /// Defines client-editable filtering criteria for role option queries.
    /// </summary>
namespace skolenza.application.DTOS.Role
{
   
    public class GetRoleOptionRequestDTO : PermissionRequestDTO
    {

        public int? RoleType { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
