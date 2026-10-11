using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace skolenza.persistance.Repositories;

public sealed class InstitutionEmailConfigRepository(
    SkolenzaDbContext context,
    ILogger<InstitutionEmailConfigRepository> logger) : IInstitutionEmailConfigRepository
{
    public Task<InstitutionEmailConfig?> GetActiveEmailConfigAsync(long? institutionId) =>
        context.InstitutionEmailConfigs
            .Include(configuration => configuration.Institution)
                .ThenInclude(institution => institution.InstitutionProfile)
            .AsNoTracking()
            .Where(configuration => configuration.InstitutionId == institutionId && configuration.IsActive)
            .OrderBy(configuration => configuration.Id)
            .FirstOrDefaultAsync();

    public async Task<InstitutionEmailConfig?> InsertEmailConfigAsync(InstitutionEmailConfig? config)
    {
        if (config is null)
        {
            return null;
        }

        await context.InstitutionEmailConfigs.AddAsync(config);
        logger.LogInformation("Institution email configuration added to DbContext for InstitutionId: {InstitutionId}", config.InstitutionId);
        return config;
    }

    public async Task<InstitutionEmailConfig?> UpdateEmailConfigAsync(InstitutionEmailConfig? config)
    {
        if (config is null)
        {
            return null;
        }

        var existing = await context.InstitutionEmailConfigs
            .FirstOrDefaultAsync(configuration => configuration.Id == config.Id);
        if (existing is null)
        {
            return null;
        }

        existing.SmtpHost = config.SmtpHost;
        existing.SmtpPort = config.SmtpPort;
        existing.SmtpUsername = config.SmtpUsername;
        existing.SmtpPasswordEncrypted = config.SmtpPasswordEncrypted;
        existing.FromEmail = config.FromEmail;
        existing.FromName = config.FromName;
        existing.IsActive = config.IsActive;
        if (!string.IsNullOrWhiteSpace(config.SecrateKey))
        {
            existing.SecrateKey = config.SecrateKey;
        }

        logger.LogInformation("Institution email configuration updated in DbContext for Id: {InstitutionEmailConfigId}", config.Id);
        return existing;
    }

    public Task<InstitutionEmailConfig?> GetByIdAsync(
        long institutionId,
        int id,
        CancellationToken cancellationToken = default) =>
        context.InstitutionEmailConfigs
            .Include(configuration => configuration.Institution)
            .AsNoTracking()
            .FirstOrDefaultAsync(
                configuration => configuration.InstitutionId == institutionId && configuration.Id == id,
                cancellationToken);

    public Task<InstitutionEmailConfig?> GetForUpdateAsync(
        long institutionId,
        int id,
        CancellationToken cancellationToken = default) =>
        context.InstitutionEmailConfigs.FirstOrDefaultAsync(
            configuration => configuration.InstitutionId == institutionId && configuration.Id == id,
            cancellationToken);

    public async Task<IReadOnlyList<InstitutionEmailConfig>> GetAllAsync(
        long institutionId,
        CancellationToken cancellationToken = default) =>
        await context.InstitutionEmailConfigs
            .Include(configuration => configuration.Institution)
            .AsNoTracking()
            .Where(configuration => configuration.InstitutionId == institutionId)
            .OrderByDescending(configuration => configuration.IsActive)
            .ThenBy(configuration => configuration.Id)
            .ToListAsync(cancellationToken);

    public async Task DeactivateOtherActiveAsync(
        long institutionId,
        int? excludedId,
        CancellationToken cancellationToken = default)
    {
        var configurations = context.InstitutionEmailConfigs.Where(configuration =>
            configuration.InstitutionId == institutionId &&
            configuration.IsActive &&
            (!excludedId.HasValue || configuration.Id != excludedId.Value));

        var affected = await configurations.ExecuteUpdateAsync(
            setters => setters.SetProperty(configuration => configuration.IsActive, false),
            cancellationToken);

        if (affected > 0)
        {
            logger.LogInformation(
                "Deactivated {InstitutionEmailConfigCount} previous active SMTP configuration(s) for InstitutionId: {InstitutionId}",
                affected,
                institutionId);
        }
    }

    public async Task AddAsync(InstitutionEmailConfig configuration, CancellationToken cancellationToken = default) =>
        await context.InstitutionEmailConfigs.AddAsync(configuration, cancellationToken);

    public void Remove(InstitutionEmailConfig configuration) => context.InstitutionEmailConfigs.Remove(configuration);
}
