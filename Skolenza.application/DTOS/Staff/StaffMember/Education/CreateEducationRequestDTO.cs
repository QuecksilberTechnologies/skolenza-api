// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the request model for creating Education.
// ================================================================

using skolenza.application.DTOS.Common;
using skolenza.application.DTOS.Pagination;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.Staff.Education
{
    /// <summary>
    /// Represents the CreateEducationRequestDTO data transfer model.
    /// </summary>
    public class CreateEducationRequestDTO : skolenza.application.DTOs.BaseDTO.PermissionRequestDTO
    {
       
        public string? StaffMemberId { get; set; }
        public string? Degree { get; set; }
        public string? InstituteName { get; set; }
        public string? Remark { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? ScoreValue { get; set; }
        public string? GradeDivision { get; set; }
        public string? ScoreType { get; set; }
        public bool IsEducationGapBeforeDegree { get; set; }
        public double GapYears { get; set; } = 0;
        public bool HasEducationDocUploded { get; set; }
        public string? ReasonOfEducationGap { get; set; }              

        public IFormFile? EducationDocument { get; set; }
    }


}
