using skolenza.application.DTOS.TicketDTO.Ticket;
using Npgsql.Internal.Postgres;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace skolenza.application.Interfaces.IRepositories
{
    public interface ITicketGenrationRepository
    {
        Task AddAsync( skolenza.domain.Entity.Ticket ticket);
 
        Task<GetTicketResponseDTO?> GetByIdAsync(long ticketId);
    }

   

    
}
