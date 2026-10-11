using AutoMapper;
using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace skolenza.persistance.Repositories
{
    public class TicketThreadRepository : ITicketThreadRepository
    {
        private readonly SkolenzaDbContext _context;

        private readonly IMapper _mapper;
        private readonly ILogger<TicketThreadRepository> _logger;

        public TicketThreadRepository(
            SkolenzaDbContext context,
            ILogger<TicketThreadRepository> logger,
            IMapper mapper
          )
        {
            _context = context;
            _logger = logger;
            _mapper = mapper;

        }

        public async Task<bool> AddAsync(TicketThread entity)
        {
            await _context.TicketThreads.AddAsync(entity);
            return true;
        }
    }
}
