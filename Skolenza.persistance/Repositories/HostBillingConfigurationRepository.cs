using System.Data;
using System.Data.Common;
using skolenza.application.Constants;
using skolenza.application.DTOS.Billing;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces.IRepositories;
using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace skolenza.persistance.Repositories;

public sealed class HostBillingConfigurationRepository(SkolenzaDbContext context)
    : IHostBillingConfigurationRepository
{
    public async Task<HostBillingConfigurationResponseDTO?> GetAsync(CancellationToken cancellationToken)
    {
        var connection = context.Database.GetDbConnection();
        await EnsureOpenAsync(connection, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = SelectSql;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
    }

    public async Task<HostBillingConfigurationResponseDTO> UpdateAsync(
        HostBillingConfigurationRequestDTO request,
        long hostUserId,
        CancellationToken cancellationToken)
    {
        var country = request.CountryCode.Trim().ToUpperInvariant();
        if (country != BillingConstants.IndiaCountryCode)
        {
            throw new ValidationErrorException("Only India billing configuration is supported for the initial launch.");
        }

        var connection = context.Database.GetDbConnection();
        await EnsureOpenAsync(connection, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = @"
UPDATE skolenza.""HostBillingConfiguration""
SET ""SellerLegalName""=@legalName, ""SellerBillingEmail""=@email,
    ""SellerBillingPhone""=@phone, ""CountryCode""=@country, ""StateCode""=@state,
    ""PostalCode""=@postal, ""AddressLine1""=@address1, ""AddressLine2""=@address2,
    ""TaxRegistrationNumber""=@taxNumber, ""InvoicePrefix""=@invoicePrefix,
    ""PaymentRetryCount""=@retryCount, ""GracePeriodDays""=@graceDays,
    ""PaymentTimeoutMinutes""=@timeoutMinutes, ""ReconciliationLookbackDays""=@lookbackDays,
    ""IsActive""=@isActive, ""UpdatedByHostUserId""=@hostUserId,
    ""UpdatedDateTime""=CURRENT_TIMESTAMP, ""Version""=""Version""+1
WHERE ""Version""=@version
  AND ""PaymentGatewayId""=(SELECT ""Id"" FROM skolenza.""PaymentGateway"" WHERE ""GatewayCode""=@gatewayCode)";
        Add(command, "legalName", request.SellerLegalName.Trim());
        Add(command, "email", request.SellerBillingEmail.Trim());
        Add(command, "phone", Clean(request.SellerBillingPhone));
        Add(command, "country", country);
        Add(command, "state", request.StateCode.Trim().ToUpperInvariant());
        Add(command, "postal", request.PostalCode.Trim());
        Add(command, "address1", request.AddressLine1.Trim());
        Add(command, "address2", Clean(request.AddressLine2));
        Add(command, "taxNumber", request.TaxRegistrationNumber.Trim().ToUpperInvariant());
        Add(command, "invoicePrefix", request.InvoicePrefix.Trim().ToUpperInvariant());
        Add(command, "retryCount", request.PaymentRetryCount);
        Add(command, "graceDays", request.GracePeriodDays);
        Add(command, "timeoutMinutes", request.PaymentTimeoutMinutes);
        Add(command, "lookbackDays", request.ReconciliationLookbackDays);
        Add(command, "isActive", request.IsActive);
        Add(command, "hostUserId", hostUserId);
        Add(command, "version", request.Version);
        Add(command, "gatewayCode", BillingConstants.DefaultGatewayCode);
        if (await command.ExecuteNonQueryAsync(cancellationToken) != 1)
        {
            throw new ConflictException("Billing configuration changed. Refresh and try again.");
        }

        return await GetAsync(cancellationToken)
            ?? throw new NotFoundException("Billing configuration was not found after update.");
    }

    private const string SelectSql = @"
SELECT configuration.""Id"", gateway.""GatewayCode"", gateway.""GatewayName"", gateway.""Environment"",
       configuration.""SellerLegalName"", configuration.""SellerBillingEmail"", configuration.""SellerBillingPhone"",
       configuration.""CountryCode"", configuration.""StateCode"", configuration.""PostalCode"",
       configuration.""AddressLine1"", configuration.""AddressLine2"", configuration.""TaxRegistrationNumber"",
       configuration.""InvoicePrefix"", configuration.""PaymentRetryCount"", configuration.""GracePeriodDays"",
       configuration.""PaymentTimeoutMinutes"", configuration.""ReconciliationLookbackDays"",
       configuration.""IsActive"", configuration.""Version"",
       gateway.""ClientIdEnvironmentVariable"", gateway.""ClientSecretEnvironmentVariable"", gateway.""WebhookSecretEnvironmentVariable""
FROM skolenza.""HostBillingConfiguration"" configuration
JOIN skolenza.""PaymentGateway"" gateway ON gateway.""Id""=configuration.""PaymentGatewayId""
ORDER BY configuration.""Id"" LIMIT 1";

    private static HostBillingConfigurationResponseDTO Map(DbDataReader reader) => new(
        reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
        Value(reader, 4), Value(reader, 5), Value(reader, 6), reader.GetString(7), Value(reader, 8),
        Value(reader, 9), Value(reader, 10), Value(reader, 11), Value(reader, 12), reader.GetString(13),
        reader.GetInt32(14), reader.GetInt32(15), reader.GetInt32(16), reader.GetInt32(17),
        reader.GetBoolean(18), reader.GetInt32(19),
        HasEnvironmentVariable(reader.GetString(20)), HasEnvironmentVariable(reader.GetString(21)),
        HasEnvironmentVariable(reader.GetString(22)));

    private static bool HasEnvironmentVariable(string name) => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(name));
    private static string? Value(DbDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static void Add(DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }
    private static async Task EnsureOpenAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(cancellationToken);
    }
}
