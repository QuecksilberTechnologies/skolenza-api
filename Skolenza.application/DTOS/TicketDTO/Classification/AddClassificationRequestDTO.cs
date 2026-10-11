// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the request model for adding Classification.
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

namespace skolenza.application.DTOS.TicketDTO.Classification
{
    /// <summary>
    /// Represents the AddClassificationRequestDTO data transfer model.
    /// </summary>
    public class AddClassificationRequestDTO : PermissionRequestDTO
    {
        
        public string ClassificationName { get; set; } = null!;
        public string? Description { get; set; }
        public bool? IsActive { get; set; }





    }
}
