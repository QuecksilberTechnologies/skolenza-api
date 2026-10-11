using AutoMapper;
using skolenza.application.DTOS.TicketDTO.Header;
using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace skolenza.persistance.Repositories
{
    public class ThreadMessageRepository : IThreadMessageRepository
    {
        private readonly SkolenzaDbContext _context;

        private readonly IMapper _mapper;
        private readonly ILogger<ThreadMessageRepository> _logger;

        public ThreadMessageRepository(
            SkolenzaDbContext context,
            ILogger<ThreadMessageRepository> logger,
            IMapper mapper
          )
        {
            _context = context;
            _logger = logger;
            _mapper = mapper;

        }
        public async Task<bool> AddEntityAsync(ThreadMessage entity)
        {
            await _context.ThreadMessage.AddAsync(entity);
            return true;
        }
    }
}
