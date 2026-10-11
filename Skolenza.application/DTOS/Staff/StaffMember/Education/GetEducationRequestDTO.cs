// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the request model for retrieving Education.
// ================================================================


using skolenza.application.DTOS.Common;
using skolenza.application.DTOS.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.Staff.Education
{
    /// <summary>
    /// Represents the GetEducationRequestDTO data transfer model.
    /// </summary>
    public class GetEducationRequestDTO : PermissionPagedRequestDTO
    {    
       
        
        public string? StaffMemberId { get; set; }= string.Empty;
        public string? Degree { get; set; }

        public string? InstituteName { get; set; }

        public string? Remark { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }
      
        public string? ScoreValue { get; set; }
      
        public string? ScoreType { get; set; }

        public bool? EducationGap { get; set; }       
   
        public bool? IsInfoVerified { get; set; }     
        public bool HasEducationDocUploded { get; set; }             

        public bool? IsEditAllowed { get; set; } 

        public bool? IsActive { get; set; }
    }


}
