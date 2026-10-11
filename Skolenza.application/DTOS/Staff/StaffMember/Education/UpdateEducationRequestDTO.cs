// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the request model for updating Education.
// ================================================================

using skolenza.application.DTOS.Common;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.Staff.Education
{
    /// <summary>
    /// Represents the UpdateEducationRequestDTO data transfer model.
    /// </summary>
    public class UpdateEducationRequestDTO : skolenza.application.DTOs.BaseDTO.PermissionRequestDTO
    {
      
        public int Id { get; set; }      
        public string? Degree { get; set; }= string.Empty;
        public string? InstituteName { get; set; } = string.Empty;
        public string? Remark { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; } = null;
        public DateTime? EndDate { get; set; }= null;
        public string? ScoreValue { get; set; } = string.Empty;
        public string? GradeDivision { get; set; } = string.Empty;
        public string? ScoreType { get; set; } = string.Empty;
        public bool? IsEducationGapBeforeDegree { get; set; } 
        public double GapYears { get; set; } = 0;
        public bool? HasEducationDocUploded { get; set; }
        public string? ReasonOfEducationGap { get; set; }
        public IFormFile? EducationDocument { get; set; }
    }
}
