// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the request model for processing Institution Enabled Operations.
// ================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using skolenza.application.DTOS.Common;
using skolenza.domain.Entity; 
using MediatR;

namespace skolenza.application.DTOs.Institution
{
    /// <summary>
    /// Represents the InstitutionEnabledOperationsRequestDTO data transfer model.
    /// </summary>
    public class InstitutionEnabledOperationsRequestDTO
    {
       public required bool IsActive { get; set; }
        

    }
}
