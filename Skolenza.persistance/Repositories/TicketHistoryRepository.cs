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
    public class TicketHistoryRepository : ITicketHistoryRepository
    {
        private readonly SkolenzaDbContext _context;

        private readonly IMapper _mapper;
        private readonly ILogger<TicketHistoryRepository> _logger;

        public TicketHistoryRepository(
            SkolenzaDbContext context,
            ILogger<TicketHistoryRepository> logger,
            IMapper mapper
          )
        {
            _context = context;
            _logger = logger;
            _mapper = mapper;

        }

        public async Task<bool> AddEntityAsync(TicketHistory entity)
        {
            await _context.TicketHistories.AddAsync(entity);
            return true;
        }
    }
}
