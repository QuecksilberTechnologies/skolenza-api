using skolenza.domain.Entity;
using Npgsql.Internal.Postgres;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace skolenza.application.Interfaces.IRepositories
{
    public interface ITicketAttachmentRepository
    {
        Task<bool> AddAsync(TicketAttachment entity);
        Task<TicketAttachment> GetByIdAsync(long id);
    }

   

    
}
