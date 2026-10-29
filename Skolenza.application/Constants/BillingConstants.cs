namespace skolenza.application.Constants;

/// <summary>
/// Central billing defaults. Environment configuration may override operational timings.
/// </summary>
public static class BillingConstants
{
    public const string HostBillingConfigurationModuleCode = "HOST_BILLING_CONFIGURATION";
    public const string HostBillingPlanPricesModuleCode = "HOST_BILLING_PLAN_PRICES";
    public const string HostBillingTaxRulesModuleCode = "HOST_BILLING_TAX_RULES";
    public const string HostBillingTransactionsModuleCode = "HOST_BILLING_TRANSACTIONS";
    public const string HostBillingRefundsModuleCode = "HOST_BILLING_REFUNDS";
    public const string HostBillingReconciliationModuleCode = "HOST_BILLING_RECONCILIATION";
    public const string HostBillingAuditModuleCode = "HOST_BILLING_AUDIT";
    public const string HostCountryCurrencyMasterModuleCode = "HOST_COUNTRY_CURRENCY_MASTER";
    public const string DefaultGatewayCode = "CASHFREE";
    public const string IndiaCountryCode = "IN";
    public const string IndiaCurrencyCode = "INR";
    public const int PaymentRetryCount = 3;
    public const int GracePeriodDays = 1;
    public const int WebhookProcessingRetryCount = 5;
    public const int ReconciliationLookbackDays = 7;
    public const int PaymentTimeoutMinutes = 30;
}
