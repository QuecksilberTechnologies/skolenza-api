using System;
using System.Collections.Generic;
using System.Text;

namespace skolenza.application.DTOS.TicketDTO.Creation
{
    public class CreateTicketResponseDTO
    {
        public long TicketId { get; set; } = 0;
        public string TicketNumber { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string Message { get; set; } = null!;
    }
}
