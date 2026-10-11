// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the request model for updating Dependent.
// ================================================================

using skolenza.application.DTOS.Common;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.Staff.Dependent
{
    /// <summary>
    /// Represents the UpdateDependentRequestDTO data transfer model.
    /// </summary>
    public class UpdateDependentRequestDTO : skolenza.application.DTOs.BaseDTO.PermissionRequestDTO
    {
      
        public long Id { get; set; }      
        public string? DependentName { get; set; }
        public int? Relation { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public bool? IsCoveredInPolicy { get; set; }
        public bool? IsMarried { get; set; }
        public string? Remark { get; set; }
        public string? Description { get; set; }
  
        public IFormFile? ProofFile { get; set; }

    }
}


