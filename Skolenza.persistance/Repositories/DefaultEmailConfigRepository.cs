using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace skolenza.persistance.Repositories;

/// <summary>
/// Persists the Host-wide SMTP configuration that is copied to a new Institution at registration.
/// </summary>
public sealed class DefaultEmailConfigRepository(
    SkolenzaDbContext context,
    ILogger<DefaultEmailConfigRepository> logger) : IDefaultEmailConfigRepository
{
    public Task<DefaultEmailConfig?> GetActiveDefaultEmailConfigAsync(
        CancellationToken cancellationToken = default) =>
        context.DefaultEmailConfigs
            .AsNoTracking()
            .Where(configuration => configuration.IsActive && configuration.IsDefault)
            .OrderBy(configuration => configuration.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<DefaultEmailConfig?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        context.DefaultEmailConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(configuration => configuration.Id == id, cancellationToken);

    public Task<DefaultEmailConfig?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default) =>
        context.DefaultEmailConfigs
            .FirstOrDefaultAsync(configuration => configuration.Id == id, cancellationToken);

    public async Task<IReadOnlyList<DefaultEmailConfig>> GetAllAsync(
        CancellationToken cancellationToken = default) =>
        await context.DefaultEmailConfigs
            .AsNoTracking()
            .OrderByDescending(configuration => configuration.IsDefault)
            .ThenByDescending(configuration => configuration.IsActive)
            .ThenBy(configuration => configuration.ConfigName)
            .ToListAsync(cancellationToken);

    public Task<bool> ConfigNameExistsAsync(
        string configName,
        int? excludedId = null,
        CancellationToken cancellationToken = default) =>
        context.DefaultEmailConfigs.AnyAsync(
            configuration => configuration.ConfigName == configName &&
                             (!excludedId.HasValue || configuration.Id != excludedId.Value),
            cancellationToken);

    public Task<bool> HasAnotherActiveConfigAsync(
        int excludedId,
        CancellationToken cancellationToken = default) =>
        context.DefaultEmailConfigs.AnyAsync(
            configuration => configuration.IsActive && configuration.Id != excludedId,
            cancellationToken);

    public async Task ClearExistingDefaultAsync(
        int? excludedId,
        CancellationToken cancellationToken = default)
    {
        var configurations = context.DefaultEmailConfigs.Where(configuration =>
            configuration.IsDefault &&
            (!excludedId.HasValue || configuration.Id != excludedId.Value));

        var affected = await configurations.ExecuteUpdateAsync(
            setters => setters
                .SetProperty(configuration => configuration.IsDefault, false)
                .SetProperty(configuration => configuration.UpdatedDateTime, DateTime.UtcNow),
            cancellationToken);

        if (affected > 0)
        {
            logger.LogInformation("Cleared the default flag on {ConfigurationCount} prior default email configuration(s).", affected);
        }
    }

    public async Task AddAsync(DefaultEmailConfig configuration, CancellationToken cancellationToken = default) =>
        await context.DefaultEmailConfigs.AddAsync(configuration, cancellationToken);

    public void Remove(DefaultEmailConfig configuration) => context.DefaultEmailConfigs.Remove(configuration);
}
