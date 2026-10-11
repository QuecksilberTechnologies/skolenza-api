using AutoMapper;
using skolenza.application.DTOs.Institution;
using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;


using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace skolenza.persistance.Repositories
{
    public class InstitutionSubscriptionRepository : IInstitutionSubscriptionRepository
    {
        private readonly SkolenzaDbContext _context;
        private readonly ILogger<InstitutionSubscriptionRepository> _logger;
        
        private readonly IMapper _mapper;
       



        public InstitutionSubscriptionRepository(SkolenzaDbContext context, ILogger<InstitutionSubscriptionRepository> logger, IMapper mapper)
        {
            _context = context;
            _logger = logger;
            _mapper = mapper;
           // 
        }

        public async Task<InstitutionSubscription> AddInstitutionSubscriptionAsync(InstitutionSubscription subscription)
        {
            try
            {
                if (subscription == null)
                    throw new ArgumentNullException(nameof(subscription));

                await _context.InstitutionSubscriptions.AddAsync(subscription);

                _logger.LogInformation(
                    "InstitutionSubscription added to DbContext successfully for InstitutionId: {InstitutionId}",
                    subscription.InstitutionId);

                return subscription;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error while adding InstitutionSubscription to DbContext for InstitutionId: {InstitutionId}",
                    subscription?.InstitutionId);
                throw;
            }
        }

        public Task<bool> DeleteInstitutionSubscriptionAsync(long id)
        {
            throw new NotImplementedException();
        }

        public Task<List<InstitutionSubscription>> GetAllInstitutionSubscriptionsAsync(InstitutionSubscription? filter = null)
        {
            throw new NotImplementedException();
        }

        public async Task<InstitutionSubscriptionPlanResponseDTO> GetValidateInstitutionPlan(InstitutionSubscriptionPlanRequestDTO dto)
        {
            try
            {
                if (dto == null || dto.InstitutionId <= 0)
                {
                    _logger?.LogWarning("❌ Invalid DTO passed. DTO is null or InstitutionId <= 0");
                    return new InstitutionSubscriptionPlanResponseDTO();
                }

                _logger?.LogInformation("🔍 Validating institution plan for InstitutionId: {InstitutionId}", dto.InstitutionId);

                // 🔎 DB connection state trace
                var connectionState = _context.Database.GetDbConnection().State;
                _logger?.LogInformation("📡 DB Connection State before query: {State}", connectionState);

                // 🔎 Query
                var query = _context.InstitutionSubscriptions
                    .AsNoTracking()
                    .Where(x => x.InstitutionId == dto.InstitutionId && x.IsActive)
                    .Select(x => new InstitutionSubscriptionPlanResponseDTO
                    {
                        Id = x.Id,
                        InstitutionId = x.InstitutionId,
                        SubscriptionPlanId = x.SubscriptionPlanId,
                        SubscriptionStartDate = x.SubscriptionStartDate,
                        SubscriptionEndDate = x.SubscriptionEndDate,
                        IsActive = x.IsActive
                    });

                _logger?.LogInformation("📜 Generated SQL:\n{SQL}", query.ToQueryString());

                var subscription = await query.FirstOrDefaultAsync();

                if (subscription == null)
                {
                    _logger?.LogWarning("⚠ No active subscription found for InstitutionId: {InstitutionId}", dto.InstitutionId);
                    return new InstitutionSubscriptionPlanResponseDTO();
                }

                _logger?.LogInformation("✅ Active subscription found for InstitutionId: {InstitutionId}", dto.InstitutionId);

                return subscription;
            }
            catch (Exception ex)
            {
                // 🔴 FULL ERROR TRACE
                _logger?.LogError("❌ Exception occurred while validating institution plan");

                _logger?.LogError("📛 Message: {Message}", ex.Message);

                _logger?.LogError("📛 StackTrace: {StackTrace}", ex.StackTrace);

                if (ex.InnerException != null)
                {
                    _logger?.LogError("📛 InnerException: {Inner}", ex.InnerException.Message);
                }

                // 🔎 DbContext connection state
                try
                {
                    var state = _context.Database.GetDbConnection().State;
                    _logger?.LogError("📡 DB Connection State during error: {State}", state);
                }
                catch { }

                throw; // 🚨 IMPORTANT → don't swallow error
            }
        }



        public async Task<List<InstitutionSubscriptionPlanResponseDTO>> GetInstitutionSubscriptionPlanInfoAsync(InstitutionSubscriptionPlanRequestDTO dto)
    {
        try
        {
                

                if (dto == null)
            {
                _logger.LogWarning("GetInstitutionSubscriptionPlanInfoAsync called with null DTO.");
                return new List<InstitutionSubscriptionPlanResponseDTO>();
            }

            _logger.LogInformation("Fetching InstitutionSubscription records with filters: {@dto}", dto);

            // Base query
            var query = _context.InstitutionSubscriptions.AsQueryable();

            // ✅ Apply filters dynamically
            if (dto.InstitutionId>0)
                query = query.Where(x => x.InstitutionId == dto.InstitutionId);

            if (dto.Id.HasValue && dto.Id > 0)
                query = query.Where(x => x.Id == dto.Id);

            if (dto.SubscriptionPlanId.HasValue && dto.SubscriptionPlanId > 0)
                query = query.Where(x => x.SubscriptionPlanId == dto.SubscriptionPlanId);

            if (dto.StartDate.HasValue)
                query = query.Where(x => x.SubscriptionStartDate >= dto.StartDate.Value);

            if (dto.EndDate.HasValue)
                query = query.Where(x => x.SubscriptionEndDate <= dto.EndDate.Value);

            if (dto.IsActive.HasValue)
                query = query.Where(x => x.IsActive == dto.IsActive.Value);

            if (dto.IsTrial.HasValue)
                query = query.Where(x => x.IsTrial == dto.IsTrial.Value);

            // ✅ Fetch result
            var institutionSubscriptions = await query.ToListAsync();

            if (!institutionSubscriptions.Any())
            {
                _logger.LogInformation("No institution subscriptions found for given filters.");
                return new List<InstitutionSubscriptionPlanResponseDTO>();
            }

            // ✅ Map to DTO
            var result = _mapper.Map<List<InstitutionSubscriptionPlanResponseDTO>>(institutionSubscriptions);

            _logger.LogInformation("Fetched {Count} institution subscriptions.", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching InstitutionSubscription records.");
            return new List<InstitutionSubscriptionPlanResponseDTO>();
        }
    }


    public Task UpdateInstitutionSubscriptionAsync(InstitutionSubscription subscription)
        {
            throw new NotImplementedException();
        }
    }
}
