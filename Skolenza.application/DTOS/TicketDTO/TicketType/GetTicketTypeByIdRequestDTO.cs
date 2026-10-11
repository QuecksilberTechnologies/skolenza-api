using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.TicketDTO.TicketType
{
    public class GetTicketTypeByIdRequestDTO
    {
        public long Id { get; set; }
        public required bool IsActive { get; set; }
       
       
     
    }
}
