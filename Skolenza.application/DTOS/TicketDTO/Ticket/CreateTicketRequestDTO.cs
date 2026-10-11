// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the request model for creating Ticket.
// ================================================================

using skolenza.application.DTOS.Common;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace skolenza.application.DTOS.TicketDTO.Ticket
{
    /// <summary>
    /// Represents the CreateTicketRequestDTO data transfer model.
    /// </summary>
    public class CreateTicketRequestDTO
    {
        //  Classification (UI se)
        public int? TicketClassificationId { get; set; }

        public long TicketHeaderId { get; set; }

        public long TicketTypeId { get; set; }

        //  Core Info (UI se)
        public string Description { get; set; } = null!;

        public int Priority { get; set; }

        //  Request Info (UI se)
        public long? RequestedForUserId { get; set; }

        //  Optional Attachment (future ready)
        public List<string>? AttachmentUrls { get; set; }


    }

}
