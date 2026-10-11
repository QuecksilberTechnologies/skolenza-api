using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.TicketDTO.Header
{
    public class DeleteHeaderRequestDTO
 {
         public long Id { get; set; }// The ID performing  deletion
      
    }
}