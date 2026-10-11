// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the request model for retrieving Experience.
// ================================================================

using skolenza.application.Common.Helpers;
using skolenza.application.DTOS.Common;
using skolenza.application.DTOS.Pagination;
using skolenza.domain.Entity;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace skolenza.application.DTOS.Staff.Experience
{
    /// <summary>
    /// Represents the GetExperienceRequestDTO data transfer model.
    /// </summary>
    public class GetExperienceRequestDTO : PermissionPagedRequestDTO
    {
        //  Required (encoded)
        public required string StaffMemberId { get; set; } = string.Empty;
        public  string? UserStaffMemberId { get; set; }

        //  Common Props (Institution/User context)
    }



}
