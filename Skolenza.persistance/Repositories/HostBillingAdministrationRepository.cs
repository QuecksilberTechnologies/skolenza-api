using System.Data;
using System.Data.Common;
using System.Text.Json;
using skolenza.application.Common.Models;
using skolenza.application.DTOS.Billing;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces.IRepositories;
using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace skolenza.persistance.Repositories;

public sealed class HostBillingAdministrationRepository(
    SkolenzaDbContext context,
    IOptions<BillingOptions> billingOptions) : IHostBillingAdministrationRepository
{
    public async Task<IReadOnlyList<HostPlanPriceResponseDTO>> GetPlanPricesAsync(CancellationToken ct)
    {
        const string sql = """
SELECT price."Id",price."SubscriptionPlanId",plan."PlanName",price."CountryCode",price."CurrencyCode",
       price."BillingCycle",price."BaseAmount",price."EffectiveFrom",price."EffectiveTo",price."IsActive"
FROM skolenza."SubscriptionPlanPrice" price
JOIN skolenza."SubscriptionPlan" plan ON plan."Id"=price."SubscriptionPlanId"
ORDER BY plan."PlanName",price."CountryCode",price."BillingCycle",price."EffectiveFrom" DESC;
""";
        return await ReadAsync(sql, null, r => new HostPlanPriceResponseDTO(r.GetInt64(0), r.GetInt32(1), r.GetString(2), r.GetString(3).Trim(), r.GetString(4).Trim(), r.GetString(5), r.GetDecimal(6), DateOnly.FromDateTime(r.GetDateTime(7)), r.IsDBNull(8) ? null : DateOnly.FromDateTime(r.GetDateTime(8)), r.GetBoolean(9)), ct);
    }

    public async Task<HostPlanPriceResponseDTO> SavePlanPriceAsync(long? id, HostPlanPriceRequestDTO request, long hostUserId, CancellationToken ct)
    {
        ValidateDates(request.EffectiveFrom, request.EffectiveTo);
        var country = request.CountryCode.Trim().ToUpperInvariant();
        var currency = request.CurrencyCode.Trim().ToUpperInvariant();
        var connection = await OpenAsync(ct);
        await using var tx = await connection.BeginTransactionAsync(ct);
        const string conflictSql = """
SELECT EXISTS(SELECT 1 FROM skolenza."SubscriptionPlanPrice"
 WHERE "SubscriptionPlanId"=@plan AND "CountryCode"=@country AND "CurrencyCode"=@currency
   AND "BillingCycle"=@cycle AND "IsActive"=TRUE AND (CAST(@id AS bigint) IS NULL OR "Id"<>@id)
   AND "EffectiveFrom"<=COALESCE(@effectiveTo,DATE '9999-12-31')
   AND COALESCE("EffectiveTo",DATE '9999-12-31')>=@effectiveFrom);
""";
        if (await ScalarAsync<bool>(connection, tx, conflictSql, c =>
        {
            Add(c, "plan", request.SubscriptionPlanId);
            Add(c, "country", country);
            Add(c, "currency", currency);
            Add(c, "cycle", request.BillingCycle);
            Add(c, "id", id);
            Add(c, "effectiveFrom", request.EffectiveFrom.ToDateTime(TimeOnly.MinValue));
            Add(c, "effectiveTo", request.EffectiveTo?.ToDateTime(TimeOnly.MinValue));
        }, ct))
            throw new ConflictException("An active price already overlaps this plan, country, currency and billing cycle.");
        var sql = id is null ? """
INSERT INTO skolenza."SubscriptionPlanPrice" ("SubscriptionPlanId","CountryCode","CurrencyCode","BillingCycle","BaseAmount","EffectiveFrom","EffectiveTo","IsActive","AddedByHostUserId")
VALUES (@plan,@country,@currency,@cycle,@amount,@effectiveFrom,@effectiveTo,@active,@actor) RETURNING "Id";
""" : """
UPDATE skolenza."SubscriptionPlanPrice" SET "SubscriptionPlanId"=@plan,"CountryCode"=@country,"CurrencyCode"=@currency,"BillingCycle"=@cycle,"BaseAmount"=@amount,"EffectiveFrom"=@effectiveFrom,"EffectiveTo"=@effectiveTo,"IsActive"=@active,"UpdatedByHostUserId"=@actor,"UpdatedDateTime"=CURRENT_TIMESTAMP
WHERE "Id"=@id RETURNING "Id";
""";
        var savedId = await ScalarAsync<long>(connection, tx, sql, c =>
        {
            Add(c, "plan", request.SubscriptionPlanId);
            Add(c, "country", country);
            Add(c, "currency", currency);
            Add(c, "cycle", request.BillingCycle);
            Add(c, "amount", request.BaseAmount);
            Add(c, "effectiveFrom", request.EffectiveFrom.ToDateTime(TimeOnly.MinValue));
            Add(c, "effectiveTo", request.EffectiveTo?.ToDateTime(TimeOnly.MinValue));
            Add(c, "active", request.IsActive);
            Add(c, "actor", hostUserId);
            Add(c, "id", id);
        }, ct);
        await AuditAsync(connection, tx, null, "SubscriptionPlanPrice", savedId.ToString(), id is null ? "Created" : "Updated", hostUserId, ct);
        await tx.CommitAsync(ct);
        return (await GetPlanPricesAsync(ct)).Single(x => x.Id == savedId);
    }

    public Task DeactivatePlanPriceAsync(long id, long actor, CancellationToken ct) => DeactivateAsync("SubscriptionPlanPrice", id, actor, ct);

    public async Task<IReadOnlyList<HostTaxRuleResponseDTO>> GetTaxRulesAsync(CancellationToken ct)
    {
        const string sql = """SELECT "Id","CountryCode","TaxCode","TaxName","RatePercent","EffectiveFrom","EffectiveTo","IsActive","ConfigurationJson"::text FROM skolenza."BillingTaxRule" ORDER BY "CountryCode","TaxCode","EffectiveFrom" DESC;""";
        return await ReadAsync(sql, null, r => new HostTaxRuleResponseDTO(r.GetInt64(0), r.GetString(1).Trim(), r.GetString(2), r.GetString(3), r.GetDecimal(4), DateOnly.FromDateTime(r.GetDateTime(5)), r.IsDBNull(6) ? null : DateOnly.FromDateTime(r.GetDateTime(6)), r.GetBoolean(7), Text(r, 8)), ct);
    }

    public async Task<HostTaxRuleResponseDTO> SaveTaxRuleAsync(long? id, HostTaxRuleRequestDTO request, long hostUserId, CancellationToken ct)
    {
        ValidateDates(request.EffectiveFrom, request.EffectiveTo);
        if (!string.IsNullOrWhiteSpace(request.ConfigurationJson))
        {
            try
            {
                JsonDocument.Parse(request.ConfigurationJson);
            }
            catch (JsonException)
            {
                throw new ValidationErrorException("ConfigurationJson must be valid JSON.");
            }
        }
        var country = request.CountryCode.Trim().ToUpperInvariant();
        var code = request.TaxCode.Trim().ToUpperInvariant();
        var connection = await OpenAsync(ct);
        await using var tx = await connection.BeginTransactionAsync(ct);
        const string conflictSql = """SELECT EXISTS(SELECT 1 FROM skolenza."BillingTaxRule" WHERE "CountryCode"=@country AND "TaxCode"=@code AND "IsActive"=TRUE AND (CAST(@id AS bigint) IS NULL OR "Id"<>@id) AND "EffectiveFrom"<=COALESCE(@effectiveTo,DATE '9999-12-31') AND COALESCE("EffectiveTo",DATE '9999-12-31')>=@effectiveFrom);""";
        if (await ScalarAsync<bool>(connection, tx, conflictSql, c =>
        {
            Add(c, "country", country);
            Add(c, "code", code);
            Add(c, "id", id);
            Add(c, "effectiveFrom", request.EffectiveFrom.ToDateTime(TimeOnly.MinValue));
            Add(c, "effectiveTo", request.EffectiveTo?.ToDateTime(TimeOnly.MinValue));
        }, ct)) throw new ConflictException("An active tax rule already overlaps this country and tax code.");
        var sql = id is null ? """INSERT INTO skolenza."BillingTaxRule" ("CountryCode","TaxCode","TaxName","RatePercent","EffectiveFrom","EffectiveTo","IsActive","ConfigurationJson","AddedByHostUserId") VALUES (@country,@code,@name,@rate,@effectiveFrom,@effectiveTo,@active,CAST(@json AS jsonb),@actor) RETURNING "Id";""" : """UPDATE skolenza."BillingTaxRule" SET "CountryCode"=@country,"TaxCode"=@code,"TaxName"=@name,"RatePercent"=@rate,"EffectiveFrom"=@effectiveFrom,"EffectiveTo"=@effectiveTo,"IsActive"=@active,"ConfigurationJson"=CAST(@json AS jsonb),"UpdatedByHostUserId"=@actor,"UpdatedDateTime"=CURRENT_TIMESTAMP WHERE "Id"=@id RETURNING "Id";""";
        var savedId = await ScalarAsync<long>(connection, tx, sql, c =>
        {
            Add(c, "country", country);
            Add(c, "code", code);
            Add(c, "name", request.TaxName.Trim());
            Add(c, "rate", request.RatePercent);
            Add(c, "effectiveFrom", request.EffectiveFrom.ToDateTime(TimeOnly.MinValue));
            Add(c, "effectiveTo", request.EffectiveTo?.ToDateTime(TimeOnly.MinValue));
            Add(c, "active", request.IsActive);
            Add(c, "json", string.IsNullOrWhiteSpace(request.ConfigurationJson) ? null : request.ConfigurationJson);
            Add(c, "actor", hostUserId);
            Add(c, "id", id);
        }, ct);
        await AuditAsync(connection, tx, null, "BillingTaxRule", savedId.ToString(), id is null ? "Created" : "Updated", hostUserId, ct);
        await tx.CommitAsync(ct);
        return (await GetTaxRulesAsync(ct)).Single(x => x.Id == savedId);
    }

    public Task DeactivateTaxRuleAsync(long id, long actor, CancellationToken ct) => DeactivateAsync("BillingTaxRule", id, actor, ct);

    public async Task<BillingPageResult<HostPaymentTransactionResponseDTO>> GetTransactionsAsync(HostBillingListRequestDTO request, CancellationToken ct)
    {
        var where = "WHERE (CAST(@status AS text) IS NULL OR transaction.\"Status\"=@status) AND (CAST(@search AS text) IS NULL OR institution.\"CompanyName\" ILIKE '%'||@search||'%' OR COALESCE(transaction.\"GatewayPaymentId\",'') ILIKE '%'||@search||'%')";
        var count = $"SELECT COUNT(*) FROM skolenza.\"PaymentTransaction\" transaction JOIN skolenza.\"BillingOrder\" orders ON orders.\"Id\"=transaction.\"BillingOrderId\" JOIN skolenza.\"Institution\" institution ON institution.\"Id\"=orders.\"InstitutionId\" {where}";
        var sql = $"""SELECT transaction."Id",transaction."BillingOrderId",orders."InstitutionId",institution."CompanyName",transaction."Status",transaction."PaymentMethod",transaction."CurrencyCode",transaction."Amount",transaction."GatewayPaymentId",transaction."BankReference",transaction."FailureCode",transaction."FailureMessage",transaction."PaidAt",transaction."AddedDateTime" FROM skolenza."PaymentTransaction" transaction JOIN skolenza."BillingOrder" orders ON orders."Id"=transaction."BillingOrderId" JOIN skolenza."Institution" institution ON institution."Id"=orders."InstitutionId" {where} ORDER BY transaction."AddedDateTime" DESC OFFSET @offset LIMIT @limit;""";
        Action<DbCommand> bind = c =>
        {
            Add(c, "status", Clean(request.Status));
            Add(c, "search", Clean(request.Search));
            Add(c, "offset", (request.PageNumber - 1) * request.PageSize);
            Add(c, "limit", request.PageSize);
        };
        var total = await CountAsync(count, bind, ct);
        var items = await ReadAsync(sql, bind, r => new HostPaymentTransactionResponseDTO(r.GetInt64(0), r.GetGuid(1), r.GetInt64(2), Text(r, 3), r.GetString(4), Text(r, 5), r.GetString(6).Trim(), r.GetDecimal(7), Text(r, 8), Text(r, 9), Text(r, 10), Text(r, 11), Offset(r, 12), r.GetFieldValue<DateTimeOffset>(13)), ct);
        return new(items, request.PageNumber, request.PageSize, total);
    }

    public async Task<BillingPageResult<HostRefundResponseDTO>> GetRefundsAsync(HostBillingListRequestDTO request, CancellationToken ct)
    {
        var where = "WHERE (CAST(@status AS text) IS NULL OR refund.\"Status\"=@status)";
        var count = $"SELECT COUNT(*) FROM skolenza.\"BillingRefund\" refund {where}";
        var sql = $"""SELECT refund."Id",refund."PaymentTransactionId",orders."InstitutionId",refund."Amount",refund."CurrencyCode",refund."Reason",refund."Status",refund."GatewayRefundId",refund."RequestedByHostUserId",refund."RequestedAt",refund."CompletedAt",refund."FailureMessage" FROM skolenza."BillingRefund" refund JOIN skolenza."PaymentTransaction" transaction ON transaction."Id"=refund."PaymentTransactionId" JOIN skolenza."BillingOrder" orders ON orders."Id"=transaction."BillingOrderId" {where} ORDER BY refund."RequestedAt" DESC OFFSET @offset LIMIT @limit;""";
        Action<DbCommand> bind = c =>
        {
            Add(c, "status", Clean(request.Status));
            Add(c, "offset", (request.PageNumber - 1) * request.PageSize);
            Add(c, "limit", request.PageSize);
        };
        var total = await CountAsync(count, bind, ct);
        var items = await ReadAsync(sql, bind, MapRefund, ct);
        return new(items, request.PageNumber, request.PageSize, total);
    }

    public async Task<HostRefundResponseDTO> RequestRefundAsync(HostRefundRequestDTO request, long hostUserId, CancellationToken ct)
    {
        if (request.IdempotencyKey == Guid.Empty) throw new ValidationErrorException("IdempotencyKey is required.");
        var connection = await OpenAsync(ct);
        await using var tx = await connection.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        const string existing = """SELECT refund."Id",refund."PaymentTransactionId",orders."InstitutionId",refund."Amount",refund."CurrencyCode",refund."Reason",refund."Status",refund."GatewayRefundId",refund."RequestedByHostUserId",refund."RequestedAt",refund."CompletedAt",refund."FailureMessage" FROM skolenza."BillingRefund" refund JOIN skolenza."PaymentTransaction" transaction ON transaction."Id"=refund."PaymentTransactionId" JOIN skolenza."BillingOrder" orders ON orders."Id"=transaction."BillingOrderId" WHERE refund."IdempotencyKey"=@key;""";
        var prior = await ReadOneAsync(connection, tx, existing, c => Add(c, "key", request.IdempotencyKey), MapRefund, ct);
        if (prior is not null)
        {
            await tx.CommitAsync(ct);
            return prior;
        }
        const string paymentSql = """SELECT transaction."Amount",transaction."CurrencyCode",transaction."Status",orders."InstitutionId",COALESCE((SELECT SUM(refund."Amount") FROM skolenza."BillingRefund" refund WHERE refund."PaymentTransactionId"=transaction."Id" AND refund."Status" IN ('Requested','Processing','Succeeded')),0) FROM skolenza."PaymentTransaction" transaction JOIN skolenza."BillingOrder" orders ON orders."Id"=transaction."BillingOrderId" WHERE transaction."Id"=@id FOR UPDATE;""";
        await using var payment = await ReaderAsync(connection, tx, paymentSql, c => Add(c, "id", request.PaymentTransactionId), ct);
        if (!await payment.ReadAsync(ct)) throw new NotFoundException("Payment transaction was not found.");
        var paid = payment.GetDecimal(0);
        var currency = payment.GetString(1).Trim();
        var status = payment.GetString(2);
        var institutionId = payment.GetInt64(3);
        var refunded = payment.GetDecimal(4);
        await payment.DisposeAsync();
        if (status is not ("Succeeded" or "PartiallyRefunded")) throw new ConflictException("Only a successful payment can be refunded.");
        if (request.Amount > paid - refunded) throw new ConflictException("Refund amount exceeds the remaining refundable balance.");
        var refundId = Guid.NewGuid();
        const string insert = """INSERT INTO skolenza."BillingRefund" ("Id","PaymentTransactionId","IdempotencyKey","Amount","CurrencyCode","Reason","Status","RequestedByHostUserId") VALUES (@id,@payment,@key,@amount,@currency,@reason,'Requested',@actor);""";
        await ExecuteAsync(connection, tx, insert, c =>
        {
            Add(c, "id", refundId);
            Add(c, "payment", request.PaymentTransactionId);
            Add(c, "key", request.IdempotencyKey);
            Add(c, "amount", request.Amount);
            Add(c, "currency", currency);
            Add(c, "reason", request.Reason.Trim());
            Add(c, "actor", hostUserId);
        }, ct);
        await AuditAsync(connection, tx, institutionId, "BillingRefund", refundId.ToString(), "Requested", hostUserId, ct);
        await tx.CommitAsync(ct);
        return new(refundId, request.PaymentTransactionId, institutionId, request.Amount, currency, request.Reason.Trim(), "Requested", null, hostUserId, DateTimeOffset.UtcNow, null, null);
    }

    public async Task<HostReconciliationSummaryDTO> GetReconciliationAsync(CancellationToken ct)
    {
        const string sql = """SELECT (SELECT COUNT(*) FROM skolenza."BillingOrder" WHERE "Status" IN ('Created','Pending') AND "ExpiresAt"<CURRENT_TIMESTAMP),(SELECT COUNT(*) FROM skolenza."PaymentWebhookEvent" WHERE "ProcessingStatus"='Failed'),(SELECT COUNT(*) FROM skolenza."PaymentWebhookEvent" WHERE "ProcessingStatus"='Pending'),(SELECT COUNT(*) FROM skolenza."PaymentAttempt" WHERE "Status"='Failed'),(SELECT COUNT(*) FROM skolenza."BillingRefund" WHERE "Status"='Requested');""";
        var rows = await ReadAsync(sql, null, r => new HostReconciliationSummaryDTO(Convert.ToInt32(r.GetInt64(0)), Convert.ToInt32(r.GetInt64(1)), Convert.ToInt32(r.GetInt64(2)), Convert.ToInt32(r.GetInt64(3)), Convert.ToInt32(r.GetInt64(4)), DateTimeOffset.UtcNow), ct);
        return rows[0];
    }

    public async Task RetryWebhookAsync(long webhookEventId, long hostUserId, CancellationToken ct)
    {
        var connection = await OpenAsync(ct);
        await using var tx = await connection.BeginTransactionAsync(ct);
        const string sql = """
UPDATE skolenza."PaymentWebhookEvent" event
SET "ProcessingStatus"='Pending', "ErrorMessage"=NULL
WHERE event."Id"=@id AND event."ProcessingStatus"='Failed' AND event."RetryCount"<@retryLimit
RETURNING event."Id";
""";
        var id = await ScalarNullableAsync<long>(connection, tx, sql, command =>
        {
            Add(command, "id", webhookEventId);
            Add(command, "retryLimit", billingOptions.Value.WebhookProcessingRetryCount);
        }, ct);
        if (id is null)
        {
            throw new ConflictException("Webhook is not failed or has reached its retry limit.");
        }

        await AuditAsync(connection, tx, null, "PaymentWebhookEvent", webhookEventId.ToString(), "RetryQueued", hostUserId, ct);
        await tx.CommitAsync(ct);
    }

    public async Task<BillingPageResult<HostBillingAuditResponseDTO>> GetAuditAsync(HostBillingListRequestDTO request, CancellationToken ct)
    {
        var where = "WHERE (CAST(@search AS text) IS NULL OR \"EntityType\" ILIKE '%'||@search||'%' OR \"EntityId\" ILIKE '%'||@search||'%' OR \"Action\" ILIKE '%'||@search||'%')";
        var count = $"SELECT COUNT(*) FROM skolenza.\"BillingAuditLog\" {where}";
        var sql = $"""SELECT "Id","InstitutionId","EntityType","EntityId","Action","OldStatus","NewStatus","ActorType","ActorId","CorrelationId","MetadataJson"::text,"AddedDateTime" FROM skolenza."BillingAuditLog" {where} ORDER BY "AddedDateTime" DESC OFFSET @offset LIMIT @limit;""";
        Action<DbCommand> bind = c =>
        {
            Add(c, "search", Clean(request.Search));
            Add(c, "offset", (request.PageNumber - 1) * request.PageSize);
            Add(c, "limit", request.PageSize);
        };
        var total = await CountAsync(count, bind, ct);
        var items = await ReadAsync(sql, bind, r => new HostBillingAuditResponseDTO(r.GetInt64(0), r.IsDBNull(1) ? null : r.GetInt64(1), r.GetString(2), r.GetString(3), r.GetString(4), Text(r, 5), Text(r, 6), r.GetString(7), r.IsDBNull(8) ? null : r.GetInt64(8), Text(r, 9), Text(r, 10), r.GetFieldValue<DateTimeOffset>(11)), ct);
        return new(items, request.PageNumber, request.PageSize, total);
    }

    private async Task DeactivateAsync(string table, long id, long actor, CancellationToken ct)
    {
        if (table is not ("SubscriptionPlanPrice" or "BillingTaxRule")) throw new InvalidOperationException();
        var c = await OpenAsync(ct);
        await using var tx = await c.BeginTransactionAsync(ct);
        var sql = $"UPDATE skolenza.\"{table}\" SET \"IsActive\"=FALSE,\"UpdatedByHostUserId\"=@actor,\"UpdatedDateTime\"=CURRENT_TIMESTAMP WHERE \"Id\"=@id AND \"IsActive\"=TRUE;";
        if (await ExecuteAsync(c, tx, sql, x =>
        {
            Add(x, "id", id);
            Add(x, "actor", actor);
        }, ct) != 1) throw new NotFoundException($"{table} was not found or is already inactive.");
        await AuditAsync(c, tx, null, table, id.ToString(), "Deactivated", actor, ct);
        await tx.CommitAsync(ct);
    }
    private static void ValidateDates(DateOnly from, DateOnly? to)
    {
        if (from == default) throw new ValidationErrorException("EffectiveFrom is required.");
        if (to < from) throw new ValidationErrorException("EffectiveTo cannot be before EffectiveFrom.");
    }
    private async Task<DbConnection> OpenAsync(CancellationToken ct)
    {
        var c = context.Database.GetDbConnection();
        if (c.State != ConnectionState.Open) await c.OpenAsync(ct);
        return c;
    }
    private async Task<IReadOnlyList<T>> ReadAsync<T>(string sql, Action<DbCommand>? bind, Func<DbDataReader, T> map, CancellationToken ct)
    {
        var c = await OpenAsync(ct);
        await using var cmd = c.CreateCommand();
        cmd.CommandText = sql;
        bind?.Invoke(cmd);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        var list = new List<T>();
        while (await r.ReadAsync(ct)) list.Add(map(r));
        return list;
    }
    private async Task<int> CountAsync(string sql, Action<DbCommand> bind, CancellationToken ct)
    {
        var c = await OpenAsync(ct);
        await using var cmd = c.CreateCommand();
        cmd.CommandText = sql;
        bind(cmd);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
    }
    private static async Task<DbDataReader> ReaderAsync(DbConnection c, DbTransaction tx, string sql, Action<DbCommand> bind, CancellationToken ct)
    {
        var cmd = c.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = sql;
        bind(cmd);
        return await cmd.ExecuteReaderAsync(ct);
    }
    private static async Task<T?> ReadOneAsync<T>(DbConnection c, DbTransaction tx, string sql, Action<DbCommand> bind, Func<DbDataReader, T> map, CancellationToken ct) where T : class
    {
        await using var cmd = c.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = sql;
        bind(cmd);
        await using var r = await cmd.ExecuteReaderAsync(ct);
        return await r.ReadAsync(ct) ? map(r) : null;
    }
    private static async Task<T> ScalarAsync<T>(DbConnection c, DbTransaction tx, string sql, Action<DbCommand> bind, CancellationToken ct)
    {
        await using var cmd = c.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = sql;
        bind(cmd);
        var value = await cmd.ExecuteScalarAsync(ct);
        if (value is null || value is DBNull) throw new NotFoundException("The requested billing record was not found.");
        return (T)Convert.ChangeType(value, typeof(T));
    }
    private static async Task<T?> ScalarNullableAsync<T>(DbConnection c, DbTransaction tx, string sql, Action<DbCommand> bind, CancellationToken ct) where T : struct
    {
        await using var cmd = c.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = sql;
        bind(cmd);
        var value = await cmd.ExecuteScalarAsync(ct);
        return value is null || value is DBNull ? null : (T)Convert.ChangeType(value, typeof(T));
    }
    private static async Task<int> ExecuteAsync(DbConnection c, DbTransaction tx, string sql, Action<DbCommand> bind, CancellationToken ct)
    {
        await using var cmd = c.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = sql;
        bind(cmd);
        return await cmd.ExecuteNonQueryAsync(ct);
    }
    private static Task AuditAsync(DbConnection c, DbTransaction tx, long? institution, string type, string id, string action, long actor, CancellationToken ct) => ExecuteAsync(c, tx, """INSERT INTO skolenza."BillingAuditLog" ("InstitutionId","EntityType","EntityId","Action","ActorType","ActorId") VALUES (@institution,@type,@id,@action,'HostUser',@actor);""", x =>
    {
        Add(x, "institution", institution);
        Add(x, "type", type);
        Add(x, "id", id);
        Add(x, "action", action);
        Add(x, "actor", actor);
    }, ct);
    private static HostRefundResponseDTO MapRefund(DbDataReader r) => new(r.GetGuid(0), r.GetInt64(1), r.GetInt64(2), r.GetDecimal(3), r.GetString(4).Trim(), r.GetString(5), r.GetString(6), Text(r, 7), r.GetInt64(8), r.GetFieldValue<DateTimeOffset>(9), Offset(r, 10), Text(r, 11));
    private static string? Text(DbDataReader r, int i) => r.IsDBNull(i) ? null : r.GetString(i);
    private static DateTimeOffset? Offset(DbDataReader r, int i) => r.IsDBNull(i) ? null : r.GetFieldValue<DateTimeOffset>(i);
    private static string? Clean(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    private static void Add(DbCommand c, string name, object? value)
    {
        var p = c.CreateParameter();
        p.ParameterName = name;
        p.Value = value ?? DBNull.Value;
        c.Parameters.Add(p);
    }
}


