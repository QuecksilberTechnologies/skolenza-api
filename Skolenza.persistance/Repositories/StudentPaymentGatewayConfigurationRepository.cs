using skolenza.application.Common.Enums;
using skolenza.application.Constants;
using skolenza.application.DTOS.Student;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace skolenza.persistance.Repositories;

public sealed class StudentPaymentGatewayConfigurationRepository(SkolenzaDbContext context)
    : IStudentPaymentGatewayConfigurationRepository
{
    public async Task<StudentPaymentGatewayConfigurationResponseDTO?> GetAsync(
        long institutionId,
        CancellationToken cancellationToken)
    {
        var configuration = await context.StudentPaymentGatewayConfigurations.AsNoTracking()
            .SingleOrDefaultAsync(item => item.InstitutionId == institutionId &&
                item.GatewayCode == StudentConstants.CashfreeGatewayCode, cancellationToken);
        return configuration is null ? null : Map(configuration);
    }

    public Task<StudentPaymentGatewayConfiguration?> GetCredentialsAsync(
        long institutionId,
        CancellationToken cancellationToken) =>
        context.StudentPaymentGatewayConfigurations.AsNoTracking()
            .SingleOrDefaultAsync(item => item.InstitutionId == institutionId &&
                item.GatewayCode == StudentConstants.CashfreeGatewayCode && item.IsActive, cancellationToken);

    public async Task<StudentPaymentGatewayConfigurationResponseDTO> CreateAsync(
        StudentPaymentGatewayConfiguration configuration,
        CancellationToken cancellationToken)
    {
        if (await context.StudentPaymentGatewayConfigurations.AnyAsync(item =>
                item.InstitutionId == configuration.InstitutionId &&
                item.GatewayCode == StudentConstants.CashfreeGatewayCode, cancellationToken))
        {
            throw new ConflictException("Cashfree tuition gateway configuration already exists for this institution.");
        }

        configuration.GatewayCode = StudentConstants.CashfreeGatewayCode;
        configuration.Version = 1;
        context.StudentPaymentGatewayConfigurations.Add(configuration);
        await context.SaveChangesAsync(cancellationToken);
        return Map(configuration);
    }

    public async Task<StudentPaymentGatewayConfigurationResponseDTO> UpdateAsync(
        StudentPaymentGatewayConfiguration configuration,
        int expectedVersion,
        CancellationToken cancellationToken)
    {
        var current = await context.StudentPaymentGatewayConfigurations.SingleOrDefaultAsync(item =>
            item.InstitutionId == configuration.InstitutionId &&
            item.GatewayCode == StudentConstants.CashfreeGatewayCode, cancellationToken)
            ?? throw new NotFoundException("Cashfree tuition gateway configuration was not found.");
        if (current.Version != expectedVersion)
        {
            throw new ConflictException("Cashfree gateway configuration changed. Refresh and try again.");
        }

        current.Environment = configuration.Environment;
        current.ClientId = configuration.ClientId;
        current.ProtectedClientSecret = configuration.ProtectedClientSecret;
        current.ProtectedWebhookSecret = configuration.ProtectedWebhookSecret;
        current.IsActive = configuration.IsActive;
        current.UpdatedById = configuration.UpdatedById;
        current.UpdatedDateTime = DateTime.UtcNow;
        current.Version++;
        await context.SaveChangesAsync(cancellationToken);
        return Map(current);
    }

    private static StudentPaymentGatewayConfigurationResponseDTO Map(StudentPaymentGatewayConfiguration entity) => new()
    {
        GatewayCode = entity.GatewayCode,
        Environment = (StudentPaymentGatewayEnvironment)entity.Environment,
        ClientIdMasked = Mask(entity.ClientId),
        HasClientSecret = !string.IsNullOrWhiteSpace(entity.ProtectedClientSecret),
        HasWebhookSecret = !string.IsNullOrWhiteSpace(entity.ProtectedWebhookSecret),
        IsActive = entity.IsActive,
        Version = entity.Version,
        UpdatedAt = entity.UpdatedDateTime.HasValue
            ? new DateTimeOffset(DateTime.SpecifyKind(entity.UpdatedDateTime.Value, DateTimeKind.Utc))
            : null
    };

    private static string Mask(string value) => value.Length <= 4
        ? new string('*', value.Length)
        : string.Concat(new string('*', Math.Max(0, value.Length - 4)), value[^4..]);
}
