// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the request model for updating Ticket Type.
// ================================================================

using skolenza.application.DTOS.Common;
using skolenza.domain.Entity;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace skolenza.application.DTOS.TicketDTO.TicketType
{
    /// <summary>
    /// Represents the UpdateTicketTypeRequestDTO data transfer model.
    /// </summary>
    public class UpdateTicketTypeRequestDTO
    {
        public long Id { get; set; }             // Existing TicketType Id
        public string TicketTypeName { get; set; } = null!;

        public long TicketHeaderId { get; set; }

        public string? Description { get; set; }

        //  Responsible Execution
        public int ResponsibleRoleId { get; set; }

        //  Approval Engine
        public bool IsApprovalRequired { get; set; }

        public int? ApprovalRoleId { get; set; }
        public bool IsAttachmentRequired { get; set; }

        public bool AutoApproveIfSameRole { get; set; }

        //  SLA
        public int? SLAHours { get; set; }

        //  Extra Config
        public bool IsActiveForAllUsers { get; set; } = true;

        //  Common Request (Tumhare pattern ke hisaab se)

    }

}
