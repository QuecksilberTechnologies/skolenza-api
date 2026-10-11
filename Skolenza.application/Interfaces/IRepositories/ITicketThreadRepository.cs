using skolenza.domain.Entity;
using System;
using System.Collections.Generic;
using System.Text;

namespace skolenza.application.Interfaces.IRepositories
{
    public interface ITicketThreadRepository
    {
        Task<bool> AddAsync(TicketThread thread);
    }
}
