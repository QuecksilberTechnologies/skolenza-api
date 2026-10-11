using AutoMapper;
using skolenza.application.DTOS.TicketDTO.Ticket;
using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace skolenza.persistance.Repositories
{
    public class TicketGenrationRepository : ITicketGenrationRepository
    {
        private readonly SkolenzaDbContext _context;

        private readonly IMapper _mapper;
        private readonly ILogger<TicketGenrationRepository> _logger;

        public TicketGenrationRepository(
            SkolenzaDbContext context,
            ILogger<TicketGenrationRepository> logger,
            IMapper mapper)

        {
            _context = context;
            _logger = logger;
            _mapper = mapper;

        }
        public async Task<bool> AddAsync(Ticket entity)
        {
            await _context.Ticket.AddAsync(entity);
            return true;
        }

      
        Task ITicketGenrationRepository.AddAsync(Ticket ticket)
        {
            return AddAsync(ticket);
        }

        public async Task<GetTicketResponseDTO?> GetByIdAsync(long id)
        {
            var result =
                from t in _context.Ticket.AsNoTracking()

                join tt in _context.TicketTypes
                    on t.TicketTypeId equals tt.Id into ttj
                from tt in ttj.DefaultIfEmpty()

                join th in _context.TicketHeaders
                    on t.TicketHeaderId equals th.Id into thj
                from th in thj.DefaultIfEmpty()

                join tc in _context.TicketClassifications
                    on t.TicketClassificationId equals tc.Id into tcj
                from tc in tcj.DefaultIfEmpty()

                where t.Id == id

                select new GetTicketResponseDTO
                {
                    Id = t.Id,
                    TicketNumber = t.TicketNumber,

                    TicketClassificationId = t.TicketClassificationId,
                    TicketClassificationName = tc != null ? tc.ClassificationName : null,

                    TicketHeaderId = t.TicketHeaderId,
                    TicketHeaderName = th != null ? th.HeaderName : null,

                    TicketTypeId = t.TicketTypeId,
                    TicketTypeName = tt != null ? tt.TicketTypeName : null,

                    Description = t.Description,
                    Priority = t.Priority,
                    Status = t.Status,

                    AssignedToRoleId = t.AssignedToRoleId,
                    AssignedToUserId = t.AssignedToUserId,

                    RequestedForUserId = t.RequestedForUserId,
                    RequestedByUserId = t.RequestedByUserId,

                    IsApproved = t.IsApproved,
                    ApprovedByUserId = t.ApprovedByUserId,
                    ApprovedDateTime = t.ApprovedDateTime,

                    SLAHours = t.SLAHoursSnapshot,
                    SLAStartTime = t.SLAStartTime,
                    SLAEndTime = t.SLAEndTime,
                    IsSLABreached = t.IsSLABreached,

                    AddedDateTime = t.AddedDateTime
                };

            return await result.FirstOrDefaultAsync();
        }
    }
}
