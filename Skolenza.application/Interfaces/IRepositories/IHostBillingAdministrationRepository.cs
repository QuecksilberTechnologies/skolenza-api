using skolenza.application.DTOS.Billing;

namespace skolenza.application.Interfaces.IRepositories;

public interface IHostBillingAdministrationRepository
{
    Task<IReadOnlyList<HostPlanPriceResponseDTO>> GetPlanPricesAsync(CancellationToken cancellationToken);
    Task<HostPlanPriceResponseDTO> SavePlanPriceAsync(long? id, HostPlanPriceRequestDTO request, long hostUserId, CancellationToken cancellationToken);
    Task DeactivatePlanPriceAsync(long id, long hostUserId, CancellationToken cancellationToken);
    Task<IReadOnlyList<HostTaxRuleResponseDTO>> GetTaxRulesAsync(CancellationToken cancellationToken);
    Task<HostTaxRuleResponseDTO> SaveTaxRuleAsync(long? id, HostTaxRuleRequestDTO request, long hostUserId, CancellationToken cancellationToken);
    Task DeactivateTaxRuleAsync(long id, long hostUserId, CancellationToken cancellationToken);
    Task<BillingPageResult<HostPaymentTransactionResponseDTO>> GetTransactionsAsync(HostBillingListRequestDTO request, CancellationToken cancellationToken);
    Task<BillingPageResult<HostRefundResponseDTO>> GetRefundsAsync(HostBillingListRequestDTO request, CancellationToken cancellationToken);
    Task<HostRefundResponseDTO> RequestRefundAsync(HostRefundRequestDTO request, long hostUserId, CancellationToken cancellationToken);
    Task<HostReconciliationSummaryDTO> GetReconciliationAsync(CancellationToken cancellationToken);
    Task RetryWebhookAsync(long webhookEventId, long hostUserId, CancellationToken cancellationToken);
    Task<BillingPageResult<HostBillingAuditResponseDTO>> GetAuditAsync(HostBillingListRequestDTO request, CancellationToken cancellationToken);
}
