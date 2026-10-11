using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.StoreProcedures
{
    public class GetStaffMemberIdentityResponseDTO
    {
        /// <summary>
        /// Row identifier (can be NULL for non-filled required docs)
        /// </summary>
        public long? Id { get; set; }

        /// <summary>
        /// StaffMember context (NULL when identity not yet created)
        /// </summary>
        public string? StaffMemberId { get; set; }
        /// <summary>
        /// Country info
        /// </summary>
        public string CountryCode { get; set; } = string.Empty;
        public string CountryName { get; set; } = string.Empty;

        /// <summary>
        /// Identity classification
        /// </summary>
        public string IdentityCategoryName { get; set; } = string.Empty;

        /// <summary>
        /// Document master
        /// </summary>
        public int IdentityCategoryDocumentId { get; set; }
        public string DocumentCode { get; set; } = string.Empty;
        public string DocumentName { get; set; } = string.Empty;
        public string? Description { get; set; }

        /// <summary>
        /// Rule from CountryIdentityRule
        /// </summary>
        public bool IsMandatory { get; set; }

        /// <summary>
        /// StaffMemberIdentity mapping
        /// </summary>
        public long? StaffMemberIdentityId { get; set; }
        public string? IdentityValue { get; set; }

        /// <summary>
        /// Status flags
        /// </summary>
        public bool? IsVerified { get; set; }
        public bool? IsEditAllowed { get; set; }
        public bool? HasIdentityUploaded { get; set; }

        /// <summary>
        /// Validity
        /// </summary>
        public DateTime? EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }

        public bool? IsActive { get; set; }

        /// <summary>
        /// Completion of this configured document row. A saved identity value completes the row.
        /// </summary>
        public double CompletionPercentage { get; set; }
    }

    [Keyless]
    public class GetStaffMemberIdentitySp
    {
        public long? Id { get; set; }

        public long? StaffMemberId { get; set; }

        public string? CountryCode { get; set; }
        public string? CountryName { get; set; }

        public string? IdentityCategoryName { get; set; }

        public int IdentityCategoryDocumentId { get; set; }
        public string? DocumentCode { get; set; }
        public string? DocumentName { get; set; }
        public string? Description { get; set; }

        public bool IsMandatory { get; set; }

        public long? StaffMemberIdentityId { get; set; }
        public string? IdentityValue { get; set; }

        public bool? IsVerified { get; set; }
        public bool? IsEditAllowed { get; set; }
        public bool? HasIdentityUploaded { get; set; }

        public DateOnly? EffectiveFrom { get; set; }
        public DateOnly? EffectiveTo { get; set; }

        public bool? IsActive { get; set; }
    }

}
