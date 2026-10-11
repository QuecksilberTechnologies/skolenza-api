using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace skolenza.persistance.Repositories
{
    public class InstitutionEncryptionKeyRepository : IInstitutionEncryptionKeyRepository
    {
        private readonly SkolenzaDbContext _context;
        private readonly ILogger<InstitutionEncryptionKeyRepository> _logger;

        public InstitutionEncryptionKeyRepository(
            SkolenzaDbContext context,
            ILogger<InstitutionEncryptionKeyRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task AddAsync(InstitutionEncryptionKeys institutionKey, CancellationToken cancellationToken = default)
        {
            try
            {
                if (institutionKey == null)
                {
                    throw new ArgumentNullException(nameof(institutionKey));
                }

                await _context.InstitutionEncryptionKeys.AddAsync(institutionKey, cancellationToken);

                _logger.LogInformation(
                    "Institution encryption key added to DbContext successfully for InstitutionId: {InstitutionId}",
                    institutionKey.InstitutionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error adding institution encryption key to DbContext for InstitutionId: {InstitutionId}",
                    institutionKey?.InstitutionId);
                throw;
            }
        }

        public async Task<InstitutionEncryptionKeys?> GetActiveKeyByInstitutionIdAsync(long institutionId, CancellationToken cancellationToken = default)
        {
            try
            {
                var key = await _context.InstitutionEncryptionKeys
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x => x.InstitutionId == institutionId && x.IsActive == true,
                        cancellationToken);

                if (key == null)
                {
                    _logger.LogWarning("No active encryption key found for InstitutionId: {InstitutionId}", institutionId);
                    return null;
                }

                return key;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error fetching active encryption key for InstitutionId: {InstitutionId}",
                    institutionId);
                throw;
            }
        }

        public async Task UpdateAsync(InstitutionEncryptionKeys institutionKey, CancellationToken cancellationToken = default)
        {
            try
            {
                if (institutionKey == null)
                {
                    throw new ArgumentNullException(nameof(institutionKey));
                }

                _context.InstitutionEncryptionKeys.Update(institutionKey);

                await Task.CompletedTask;

                _logger.LogInformation(
                    "Institution encryption key marked for update successfully for InstitutionId: {InstitutionId}",
                    institutionKey.InstitutionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error updating institution encryption key for InstitutionId: {InstitutionId}",
                    institutionKey?.InstitutionId);
                throw;
            }
        }
    }
}