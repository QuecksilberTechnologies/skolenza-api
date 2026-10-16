// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the request model for retrieving All StaffMember Info.
// ================================================================

using skolenza.application.Common.Helpers.Converters;
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
    public class GetAllStaffMemberInfoRequestDTO : PermissionPagedRequestDTO
    {
        /// <summary> InstitutionId Required</summary>       

  
        public string? StaffMemberId { get; set; }
        public string? EmployementCode { get; set; }
        public string? LastName { get; set; } 
        public string? FirstName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public DateTime? DateOfOnBoarding { get; set; }
        public bool? IsMarried { get; set; }   
        public int? StaffEmploymentTypeId { get; set; }   
        
        public int? GenderId { get; set; }
        public int? StaffPositionMasterId { get; set; }
        public int? DepartmentId { get; set; }
        public string? EmailId { get; set; }




    }


}
 
