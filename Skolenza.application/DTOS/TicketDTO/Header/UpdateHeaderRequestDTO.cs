// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the request model for updating Header.
// ================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using skolenza.application.DTOS.Common;
using skolenza.domain.Entity;
using MediatR;

namespace skolenza.application.DTOS.TicketDTO.Header
{
    /// <summary>
    /// Represents the UpdateHeaderRequestDTO data transfer model.
    /// </summary>
    public class UpdateHeaderRequestDTO
    {

        public long Id { get; set; }  //  encoded string (as per your rule)

        public string HeaderName { get; set; } = null!;

        public int TicketClassificationId { get; set; }

        public string? TicketClassificationName { get; set; }

        public string? Description { get; set; }

        public bool IsActive { get; set; }



    }
}
