// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the request model for updating Contact.
// ================================================================

using skolenza.application.DTOS.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.Staff.Contact
{
    /// <summary>
    /// Represents the UpdateContactRequestDTO data transfer model.
    /// </summary>
    public class UpdateContactRequestDTO : skolenza.application.DTOs.BaseDTO.PermissionRequestDTO
    {

        [Required]
        public string UserStaffMemberId { get; set; } = string.Empty;
        public string StaffMemberId { get; set; } = string.Empty;
        public long Id { get; set; }
        public string? ContactName { get; set; }
        //  Contact Info
        public int? Relation { get; set; }        // e.g. Father, Mother, Spouse
        public int? ContactType { get; set; }          // e.g. Personal, Work, Emergency
        public string? ContactNumber { get; set; }
        public string? AlternateNumber { get; set; }
        public string? Email { get; set; }
        public bool? IsPrimary { get; set; }

        //  Address Info
        public int? CountryId { get; set; }
        public int? StateId { get; set; }
        public int? DistrictId { get; set; }
        public int? LocalityId { get; set; }
        public string? HouseNo { get; set; }
        public string? LandMark { get; set; }
        public string? Street { get; set; }
        public string? Address { get; set; }
        public string? Description { get; set; }

    }

}
