using System.ComponentModel.DataAnnotations;
using skolenza.application.DTOs.BaseDTO;

namespace skolenza.application.DTOS.Billing;

public sealed class HostBillingConfigurationRequestDTO
{
    [Required] public PermissionRequestDTO PermissionRequest { get; set; } = new();
    [Required, StringLength(200)] public string SellerLegalName { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(250)] public string SellerBillingEmail { get; set; } = string.Empty;
    [Phone, StringLength(30)] public string? SellerBillingPhone { get; set; }
    [Required, StringLength(2, MinimumLength = 2)] public string CountryCode { get; set; } = "IN";
    [Required, StringLength(20)] public string StateCode { get; set; } = string.Empty;
    [Required, StringLength(20)] public string PostalCode { get; set; } = string.Empty;
    [Required, StringLength(300)] public string AddressLine1 { get; set; } = string.Empty;
    [StringLength(300)] public string? AddressLine2 { get; set; }
    [Required, StringLength(50)] public string TaxRegistrationNumber { get; set; } = string.Empty;
    [Required, StringLength(20)] public string InvoicePrefix { get; set; } = "AXP";
    [Range(1, 10)] public int PaymentRetryCount { get; set; } = Constants.BillingConstants.PaymentRetryCount;
    [Range(0, 30)] public int GracePeriodDays { get; set; } = Constants.BillingConstants.GracePeriodDays;
    [Range(5, 1440)] public int PaymentTimeoutMinutes { get; set; } = Constants.BillingConstants.PaymentTimeoutMinutes;
    [Range(1, 90)] public int ReconciliationLookbackDays { get; set; } = Constants.BillingConstants.ReconciliationLookbackDays;
    public bool IsActive { get; set; }
    [Range(1, int.MaxValue)] public int Version { get; set; } = 1;
}

public sealed record HostBillingConfigurationResponseDTO(
    int Id, string GatewayCode, string GatewayName, string Environment,
    string? SellerLegalName, string? SellerBillingEmail, string? SellerBillingPhone,
    string CountryCode, string? StateCode, string? PostalCode, string? AddressLine1,
    string? AddressLine2, string? TaxRegistrationNumber, string InvoicePrefix,
    int PaymentRetryCount, int GracePeriodDays, int PaymentTimeoutMinutes,
    int ReconciliationLookbackDays, bool IsActive, int Version,
    bool HasClientId, bool HasClientSecret, bool HasWebhookSecret);
