// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the request model for retrieving Reporting Type.
// ================================================================

using skolenza.application.DTOS.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; 
using skolenza.domain.Entity; 
using MediatR;
using skolenza.application.DTOS.Common;

namespace skolenza.application.DTOs.Manager.ReportingType
{
    /// <summary>
    /// Represents the GetReportingTypeRequestDTO data transfer model.
    /// </summary>
    public class GetReportingTypeRequestDTO: BaseRequest
    {
        
      
        public bool? IsActive { get; set; }
        public string? TypeName { get; set; }


    }
}
