// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the request model for creating Ticket.
// ================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using skolenza.application.DTOS.Common;
using skolenza.domain.Entity; 
using MediatR;

namespace skolenza.application.DTOS.TicketDTO.Creation
{
    /// <summary>
    /// Represents the CreateTicketRequestDTO data transfer model.
    /// </summary>
    public class CreateTicketRequestDTO
    {
        public int TicketClassificationId { get; set; }

        public long TicketHeaderId { get; set; }

        public long TicketTypeId { get; set; }

        public string? Description { get; set; }

        public int Priority { get; set; }

        public long RequestedForUserId { get; set; }

    }

}
