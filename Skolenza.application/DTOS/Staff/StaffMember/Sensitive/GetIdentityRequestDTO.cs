// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the request model for retrieving Identity.
// ================================================================

using skolenza.application.DTOS.Common;
using skolenza.application.DTOS.Pagination;
using System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.Staff.Sensitive
{
    /// <summary>
    /// Represents the GetIdentityRequestDTO data transfer model.
    /// </summary>
    public class GetIdentityRequestDTO : skolenza.application.DTOs.BaseDTO.PermissionRequestDTO
    {
             
      
        /// <summary>Optional encoded target staffMember; omitted for self-service compatibility.</summary>
        public string? StaffMemberId { get; set; }

        /// <summary>Legacy client hint. The persisted staffMember country controls document eligibility.</summary>
        public required int CountryNationalityId { get; set; }
    }
}
