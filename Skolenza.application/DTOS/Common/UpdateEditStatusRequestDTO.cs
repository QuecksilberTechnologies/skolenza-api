// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the request model for updating Edit Status.
// ================================================================

using System;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.Common
{
    /// <summary>
    /// Represents the UpdateEditStatusRequestDTO data transfer model.
    /// </summary>
    public class UpdateEditStatusRequestDTO : skolenza.application.DTOs.BaseDTO.PermissionRequestDTO
    {
        [Required]
        public string UserStaffMemberId { get; set; } = default!;

        [Required]
        public string StaffMemberId { get; set; } = default!;

        [Required]
        public bool IsEditable { get; set; }
    }

    /// <summary>
    /// Represents the UpdateEditStatusRequestDTO_ application component.
    /// </summary>
    public class UpdateEditStatusRequestDTO_ : skolenza.application.DTOs.BaseDTO.PermissionRequestDTO
    {
        [Required]
        public string UserStaffMemberId { get; set; } = default!;

        [Required]
        public string StaffMemberId { get; set; } = default!;
        [Required]
        public int TabInfoType { get; set; } = default!;

        [Required]
        public bool IsEditable { get; set; }

    }

}
