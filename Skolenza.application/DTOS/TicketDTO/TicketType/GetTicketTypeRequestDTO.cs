// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the request model for retrieving Ticket Type.
// ================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using skolenza.application.DTOS.Common;
using skolenza.application.DTOS.Pagination;
using skolenza.domain.Entity; 
using MediatR;

namespace skolenza.application.DTOS.TicketDTO.TicketType
{
    /// <summary>
    /// Represents the GetTicketTypeRequestDTO data transfer model.
    /// </summary>
    public class GetTicketTypeRequestDTO:BaseRequest
    {


    }
    public class GetDDLTicketTypeRequestDTO
    {

        public bool IsActive { get; set; }


    }

}
