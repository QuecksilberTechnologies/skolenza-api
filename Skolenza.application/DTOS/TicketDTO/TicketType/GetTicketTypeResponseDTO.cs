using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.TicketDTO.TicketType
{
    public class GetTicketTypeResponseDTO
    {
        public long Id { get; set; }
        public int ClassificationId { get; set; }

        public string TicketTypeName { get; set; } = null!;

        public long TicketHeaderId { get; set; }
        public string? TicketHeaderName { get; set; }

        public long InstitutionId { get; set; }

        public int? ResponsibleRoleId { get; set; }
        public string? ResponsibleRoleName { get; set; }

        public bool IsApprovalRequired { get; set; }

        public int? ApprovalId { get; set; }
        public string? ApprovalRoleName { get; set; }

        public bool AutoApproveIfSameRole { get; set; }

        public int? SLAHours { get; set; }

        public bool IsAttachmentRequired { get; set; }

        public bool IsActiveForAllUsers { get; set; }

        public string? Description { get; set; }

        public bool IsActive { get; set; }

    
    }
    public class GetTicketTypeRoleResponseDTO
    {
        public long Id { get; set; }
        public string TicketTypeName { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int ResponsibleRoleId { get; set; }
        public string ResponsibleRoleName { get; set; } = string.Empty;

        // 🔥 Approval info bhi add karo (useful for UI)
        public bool? IsApprovalRequired { get; set; }
        public int? ApprovalRoleId { get; set; }
        public string? ApprovalRoleName { get; set; }

        public List<StaffMemberMinInfoDTO> StaffMembers { get; set; } = new();
    }
    
    
    public class StaffMemberMinInfoDTO
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class GetDDLTicketTypeResponseDTO
    {
        public long Id { get; set; }

        public string TicketTypeName { get; set; } = null!;   

        public string? Description { get; set; }

        public bool IsActive { get; set; }


    }
   

}
