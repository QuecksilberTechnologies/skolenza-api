// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the request model for creating Reporting Type.
// ================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using skolenza.application.DTOS.Common;
using skolenza.application.DTOs.BaseDTO;
using skolenza.domain.Entity; 
using MediatR;

namespace skolenza.application.DTOs.Manager.ReportingType
{
    /// <summary>
    /// Represents the CreateReportingTypeRequestDTO data transfer model.
    /// </summary>
    public class CreateReportingTypeRequestDTO : PermissionRequestDTO
    {
        

        public required string TypeName { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
     


    }
}
