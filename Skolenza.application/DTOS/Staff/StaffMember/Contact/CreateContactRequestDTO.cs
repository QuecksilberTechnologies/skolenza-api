// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the request model for creating Contact.
// ================================================================

using skolenza.application.DTOS.Common;
using skolenza.application.DTOS.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;
using static skolenza.application.Constants.ConstantValues;

namespace skolenza.application.DTOS.Staff.Contact
{
    /// <summary>
    /// Represents the CreateContactRequestDTO data transfer model.
    /// </summary>
    public class CreateContactRequestDTO : skolenza.application.DTOs.BaseDTO.PermissionRequestDTO
    {

        public string StaffMemberId { get; set; } = string.Empty;

        //  Contact Info
        public string? ContactName { get; set; }          // e.g. Personal, Work, Emergency
        public ContactTypeEnum ContactType { get; set; }
        // e.g. Personal, Work, Emergency
        public string ContactNumber { get; set; } = string.Empty;
        public int? Relation { get; set; }
        public string? AlternateNumber { get; set; }
        public string? Email { get; set; }
        public bool IsPrimary { get; set; } = false;

        //  Address Info
        public int? CountryId { get; set; }
        public int? StateId { get; set; }
        public int? DistrictId { get; set; }
        public int? LocalityId { get; set; }
        public string? HouseNo { get; set; }
        public string? LandMark { get; set; }
        public string? Street { get; set; }
        public string? Address { get; set; }


        //  Optional/Metadata
        public string? Remark { get; set; }
        public string? Description { get; set; }



    }
}
