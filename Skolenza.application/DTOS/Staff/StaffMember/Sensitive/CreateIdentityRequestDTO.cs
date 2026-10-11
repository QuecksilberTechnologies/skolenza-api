// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the request model for creating Identity.
// ================================================================

using skolenza.application.DTOS.Common;
using skolenza.application.DTOS.Pagination;
using skolenza.application.Interfaces.IFileStorage;
using MediatR.NotificationPublishers;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.Staff.Sensitive
{


    /// <summary>
    /// Represents the CreateStaffMemberIdentityRequestDTO data transfer model.
    /// </summary>
    public class CreateStaffMemberIdentityRequestDTO : skolenza.application.DTOs.BaseDTO.PermissionRequestDTO
    {
        public List<CreateIdentityRequest> Identities { get; set; }
            = new List<CreateIdentityRequest>();
    }

    /// <summary>
    /// Represents the CreateIdentityRequest application component.
    /// </summary>
    public class CreateIdentityRequest
    {



        //  Core Identity Info
        public required string UserStaffMemberId { get; set; }
        public required string StaffMemberId { get; set; }
        public int IdentityCategoryDocumentId { get; set; }
        public required string IdentityValue { get; set; } = string.Empty;
        public required  string DocumnetCode  { get; set; }

        //  Document Info
        public IFormFile? IdentityDocumentFile { get; set; }
        //  Validity
        public DateOnly? EffectiveFrom { get; set; }
        public DateOnly? EffectiveTo { get; set; }

        //  Audit Fields
        // public string? DocumentFileName { get; set; }
        //public string? DocumentFilePath { get; set; }

        //  Verification Info
        //  public bool IsVerified { get; set; } = false;
        //  public long? InfoVerifiedById { get; set; }
        //  public DateTime? InfoVerifiedDateTime { get; set; }

        //  Permissions / Flags
        //  public bool IsEditAllowed { get; set; } = true;
        //  public bool HasIdentityUploaded { get; set; } = false;
        //   public bool IsActive { get; set; } = true;



    }

}


