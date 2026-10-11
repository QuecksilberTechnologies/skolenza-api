// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the request model for retrieving User Role.
// ================================================================

using skolenza.application.DTOS.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace skolenza.application.DTOS.UserRoles
{
    /// <summary>
    /// Represents the GetUserRoleRequestDTO data transfer model.
    /// </summary>
    public class GetUserRoleRequestDTO
    {
        public required string StaffMemberId { get; set; }
    }
}
