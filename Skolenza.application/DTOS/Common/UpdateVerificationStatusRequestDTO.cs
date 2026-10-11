// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the request model for updating Verification Status.
// ================================================================

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.Common
{
    /// <summary>
    /// Represents the UpdateVerificationStatusRequestDTO data transfer model.
    /// </summary>
    public class UpdateVerificationStatusRequestDTO : skolenza.application.DTOs.BaseDTO.PermissionRequestDTO
    {
        [Required]
        public string UserStaffMemberId { get; set; } = default!;

        [Required]
        public string StaffMemberId { get; set; } = default!;

        [Required]
        public bool IsVerified { get; set; }

    }
    /// <summary>
    /// Represents the UpdateVerificationStatusRequestDTO_ application component.
    /// </summary>
    public class UpdateVerificationStatusRequestDTO_ : skolenza.application.DTOs.BaseDTO.PermissionRequestDTO
    {
        [Required]
        public string UserStaffMemberId { get; set; } = default!;

        [Required]
        public string StaffMemberId { get; set; } = default!;
        [Required]
        public  int TabInfoType { get; set; } = default!;

        [Required]
        public bool IsVerified { get; set; }

    }


}

