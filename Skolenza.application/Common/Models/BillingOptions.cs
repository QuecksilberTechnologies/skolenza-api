using System.ComponentModel.DataAnnotations;

namespace skolenza.application.Common.Models;

public sealed class BillingOptions
{
    public const string SectionName = "Billing";

    [Range(1, 10)] public int PaymentRetryCount { get; set; } = Constants.BillingConstants.PaymentRetryCount;
    [Range(0, 30)] public int GracePeriodDays { get; set; } = Constants.BillingConstants.GracePeriodDays;
    [Range(1, 20)] public int WebhookProcessingRetryCount { get; set; } = Constants.BillingConstants.WebhookProcessingRetryCount;
    [Range(1, 90)] public int ReconciliationLookbackDays { get; set; } = Constants.BillingConstants.ReconciliationLookbackDays;
    [Range(5, 1440)] public int PaymentTimeoutMinutes { get; set; } = Constants.BillingConstants.PaymentTimeoutMinutes;
}
