// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the request model for retrieving Base StaffMember.
// ================================================================

using skolenza.application.DTOS.Common;
using skolenza.application.DTOS.Pagination;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.Staff.BaseStaffMember
{

    /// <summary>
    /// post-request to fetch staffMember-info  
    /// </summary>
    public class GetBaseStaffMemberRequestDTO: PermissionPagedRequestDTO
    {
        /// <summary> InstitutionId Required</summary>       
        
        public string? StaffMemberId { get; set; }
        public string? EmployementCode { get; set; }

        public string? LastName { get; set; }

        public string? FirstName { get; set; } 

        public DateTime? DateOfBirth { get; set; }

        public DateTime? DateOfOnBoarding { get; set; }
        public string? DesignationId { get; set; }  
        public bool? HasPermanent { get; set; }
        public int TypeId { get; set; }
        public bool IsActive { get; set; }
        public bool? IsEditAllowed { get; set; }  
        public bool? IsInfoVerified { get; set; }


    }


}
