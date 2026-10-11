

using skolenza.application.DTOS.TicketDTO.Header;
using skolenza.domain.Entity;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace skolenza.application.Interfaces.IRepositories
{

    public interface IThreadMessageRepository
    {
        Task<bool> AddEntityAsync(ThreadMessage entity);
       

    }

}
