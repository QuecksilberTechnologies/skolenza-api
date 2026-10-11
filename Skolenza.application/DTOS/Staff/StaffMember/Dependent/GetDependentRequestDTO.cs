// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the request model for retrieving Dependent.
// ================================================================

using skolenza.application.DTOS.Common;
using skolenza.application.DTOS.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.Staff.Dependent
{
    /// <summary>
    /// Represents the GetDependentRequestDTO data transfer model.
    /// </summary>
    public class GetDependentRequestDTO : PermissionPagedRequestDTO
    {

 
        public required string StaffMemberId { get; set; }


    }


}
