// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the request model for retrieving Ticket Type By Header Id.
// ================================================================

using skolenza.application.DTOS.Common;
using skolenza.application.DTOS.Pagination;
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
    /// Represents the GetTicketTypeByHeaderIdRequestDTO data transfer model.
    /// </summary>
    public class GetTicketTypeByHeaderIdRequestDTO 
    {
   
        public long  TicketHeaderId { get; set; }
       
    }
}
