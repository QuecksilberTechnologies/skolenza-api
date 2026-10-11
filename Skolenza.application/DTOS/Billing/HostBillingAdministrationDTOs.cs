using System.ComponentModel.DataAnnotations;
using skolenza.application.DTOs.BaseDTO;

namespace skolenza.application.DTOS.Billing;

public sealed record BillingPageResult<T>(IReadOnlyList<T> Items, int PageNumber, int PageSize, int TotalRecords);

public sealed class HostBillingListRequestDTO : PermissionRequestDTO
{
    [Range(1, int.MaxValue)] public int PageNumber { get; set; } = 1;
    [Range(1, 200)] public int PageSize { get; set; } = 20;
    [StringLength(100)] public string? Search { get; set; }
    [StringLength(30)] public string? Status { get; set; }
}

public sealed class HostPlanPriceRequestDTO
{
    [Required] public PermissionRequestDTO PermissionRequest { get; set; } = new();
    [Range(1, int.MaxValue)] public int SubscriptionPlanId { get; set; }
    [Required, StringLength(2, MinimumLength = 2)] public string CountryCode { get; set; } = "IN";
    [Required, StringLength(3, MinimumLength = 3)] public string CurrencyCode { get; set; } = "INR";
    [Required, RegularExpression("Monthly|Yearly")] public string BillingCycle { get; set; } = "Monthly";
    [Range(typeof(decimal), "0", "9999999999999999")] public decimal BaseAmount { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed record HostPlanPriceResponseDTO(long Id, int SubscriptionPlanId, string PlanName,
    string CountryCode, string CurrencyCode, string BillingCycle, decimal BaseAmount,
    DateOnly EffectiveFrom, DateOnly? EffectiveTo, bool IsActive);

public sealed class HostTaxRuleRequestDTO
{
    [Required] public PermissionRequestDTO PermissionRequest { get; set; } = new();
    [Required, StringLength(2, MinimumLength = 2)] public string CountryCode { get; set; } = "IN";
    [Required, StringLength(30)] public string TaxCode { get; set; } = "GST";
    [Required, StringLength(100)] public string TaxName { get; set; } = "Goods and Services Tax";
    [Range(typeof(decimal), "0", "100")] public decimal RatePercent { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public string? ConfigurationJson { get; set; }
}

public sealed record HostTaxRuleResponseDTO(long Id, string CountryCode, string TaxCode, string TaxName,
    decimal RatePercent, DateOnly EffectiveFrom, DateOnly? EffectiveTo, bool IsActive, string? ConfigurationJson);

public sealed record HostPaymentTransactionResponseDTO(long Id, Guid BillingOrderId, long InstitutionId,
    string? InstitutionName, string Status, string? PaymentMethod, string CurrencyCode, decimal Amount,
    string? GatewayPaymentId, string? BankReference, string? FailureCode, string? FailureMessage,
    DateTimeOffset? PaidAt, DateTimeOffset AddedDateTime);

public sealed record HostRefundResponseDTO(Guid Id, long PaymentTransactionId, long InstitutionId,
    decimal Amount, string CurrencyCode, string Reason, string Status, string? GatewayRefundId,
    long RequestedByHostUserId, DateTimeOffset RequestedAt, DateTimeOffset? CompletedAt, string? FailureMessage);

public sealed class HostRefundRequestDTO
{
    [Required] public PermissionRequestDTO PermissionRequest { get; set; } = new();
    [Range(1, long.MaxValue)] public long PaymentTransactionId { get; set; }
    [Range(typeof(decimal), "0.01", "9999999999999999")] public decimal Amount { get; set; }
    [Required, StringLength(500)] public string Reason { get; set; } = string.Empty;
    public Guid IdempotencyKey { get; set; }
}

public sealed record HostReconciliationSummaryDTO(int StalePendingOrders, int FailedWebhookEvents,
    int PendingWebhookEvents, int FailedPaymentAttempts, int RequestedRefunds, DateTimeOffset GeneratedAt);

public sealed record HostBillingAuditResponseDTO(long Id, long? InstitutionId, string EntityType,
    string EntityId, string Action, string? OldStatus, string? NewStatus, string ActorType,
    long? ActorId, string? CorrelationId, string? MetadataJson, DateTimeOffset AddedDateTime);
