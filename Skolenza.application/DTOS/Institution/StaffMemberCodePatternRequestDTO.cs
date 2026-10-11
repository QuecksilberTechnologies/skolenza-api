// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the mixed Institution and Host staffMember-code pattern request contract.
// ================================================================

using System;
using skolenza.application.DTOs.BaseDTO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.Institution
{
    /// <summary>
    /// Requests an staffMember-code pattern. Institution callers are scoped from their token; Host callers supply an encrypted Institution identifier and permission pair.
    /// </summary>
    public class StaffMemberCodePatternRequestDTO : PermissionRequestDTO
    {
        public int? Id { get; set; }
        /// <summary>Gets or sets the encrypted Institution identifier for Host callers; it is ignored for Institution callers.</summary>
        public string? InstitutionId { get; set; }
        public required bool IsActive { get; set; }
        
    }

}
