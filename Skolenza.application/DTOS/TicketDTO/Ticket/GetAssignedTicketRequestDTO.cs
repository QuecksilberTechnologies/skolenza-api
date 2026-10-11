// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the request model for retrieving Assigned Ticket.
// ================================================================

using skolenza.application.DTOS.Common;
using skolenza.application.DTOS.Pagination;
using System;
using System.Collections.Generic;
using System.Text;

namespace skolenza.application.DTOS.TicketDTO.Ticket
{

        /// <summary>
        /// Represents the GetAssignedTicketRequestDTO data transfer model.
        /// </summary>
        public class GetAssignedTicketRequestDTO : BaseRequest
        {
        public required int  Status { get; set; }   // optional (Open, Pending etc.)

        //  Login user info (CommonRequestService se aayega)

            //  Filter
        }

}
