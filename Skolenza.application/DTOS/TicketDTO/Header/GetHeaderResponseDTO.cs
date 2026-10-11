using skolenza.application.DTOS.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.TicketDTO.Header
{
    public class GetHeaderResponseDTO 
    {

       
            public long Id { get; set; }  // 🔐 encoded string (as per your rule)

            public string HeaderName { get; set; } = null!;

            public int TicketClassificationId { get; set; }

            public string? TicketClassificationName { get; set; }

            public string? Description { get; set; }

            public bool IsActive { get; set; }
        





    }
}
