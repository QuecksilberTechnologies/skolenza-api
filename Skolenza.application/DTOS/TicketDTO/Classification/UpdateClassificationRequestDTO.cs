// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the request model for updating Classification.
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
    /// Represents the UpdateClassificationRequestDTO data transfer model.
    /// </summary>
    public class UpdateClassificationRequestDTO : PermissionRequestDTO
    {
  
    public int  Id { get; set; }
    public string? ClassificationName { get; set; } 
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
   
}
}
