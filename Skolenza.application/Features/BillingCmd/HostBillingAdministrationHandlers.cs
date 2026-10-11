using skolenza.application.Constants;
using skolenza.application.DTOs.BaseDTO;
using skolenza.application.DTOS.Billing;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Wrappers;
using MediatR;

namespace skolenza.application.Features.BillingCmd;

public sealed record GetHostPlanPricesQuery(PermissionRequestDTO Permission) : IRequest<ApiResponse<IReadOnlyList<HostPlanPriceResponseDTO>>>;
public sealed record SaveHostPlanPriceCommand(long? Id, HostPlanPriceRequestDTO DTO) : IRequest<ApiResponse<HostPlanPriceResponseDTO>>;
public sealed record DeleteHostPlanPriceCommand(long Id, PermissionRequestDTO Permission) : IRequest<ApiResponse<bool>>;
public sealed record GetHostTaxRulesQuery(PermissionRequestDTO Permission) : IRequest<ApiResponse<IReadOnlyList<HostTaxRuleResponseDTO>>>;
public sealed record SaveHostTaxRuleCommand(long? Id, HostTaxRuleRequestDTO DTO) : IRequest<ApiResponse<HostTaxRuleResponseDTO>>;
public sealed record DeleteHostTaxRuleCommand(long Id, PermissionRequestDTO Permission) : IRequest<ApiResponse<bool>>;
public sealed record GetHostTransactionsQuery(HostBillingListRequestDTO DTO) : IRequest<ApiResponse<BillingPageResult<HostPaymentTransactionResponseDTO>>>;
public sealed record GetHostRefundsQuery(HostBillingListRequestDTO DTO) : IRequest<ApiResponse<BillingPageResult<HostRefundResponseDTO>>>;
public sealed record RequestHostRefundCommand(HostRefundRequestDTO DTO) : IRequest<ApiResponse<HostRefundResponseDTO>>;
public sealed record GetHostReconciliationQuery(PermissionRequestDTO Permission) : IRequest<ApiResponse<HostReconciliationSummaryDTO>>;
public sealed record RetryHostWebhookCommand(long Id, PermissionRequestDTO Permission) : IRequest<ApiResponse<bool>>;
public sealed record GetHostBillingAuditQuery(HostBillingListRequestDTO DTO) : IRequest<ApiResponse<BillingPageResult<HostBillingAuditResponseDTO>>>;

public abstract class HostBillingHandlerBase(
    ICommonRequestService common,
    IStoreProcedureRepository stored,
    string moduleCode)
{
    protected Task<long> Authorize(PermissionRequestDTO permission, string operationName, CancellationToken ct) =>
        HostBillingConfigurationAuthorization.ValidateAsync(permission, common, stored, ct, moduleCode, operationName);
}

public sealed class GetHostPlanPricesQueryHandler(
    IHostBillingAdministrationRepository repository,
    ICommonRequestService common,
    IStoreProcedureRepository stored)
    : HostBillingHandlerBase(common, stored, BillingConstants.HostBillingPlanPricesModuleCode),
      IRequestHandler<GetHostPlanPricesQuery, ApiResponse<IReadOnlyList<HostPlanPriceResponseDTO>>>
{
    public async Task<ApiResponse<IReadOnlyList<HostPlanPriceResponseDTO>>> Handle(GetHostPlanPricesQuery request, CancellationToken ct)
    {
        await Authorize(request.Permission, "View", ct);
        return ApiResponse<IReadOnlyList<HostPlanPriceResponseDTO>>.Success(await repository.GetPlanPricesAsync(ct));
    }
}

public sealed class SaveHostPlanPriceCommandHandler(
    IHostBillingAdministrationRepository repository,
    ICommonRequestService common,
    IStoreProcedureRepository stored)
    : HostBillingHandlerBase(common, stored, BillingConstants.HostBillingPlanPricesModuleCode),
      IRequestHandler<SaveHostPlanPriceCommand, ApiResponse<HostPlanPriceResponseDTO>>
{
    public async Task<ApiResponse<HostPlanPriceResponseDTO>> Handle(SaveHostPlanPriceCommand request, CancellationToken ct)
    {
        var operation = request.Id is null ? "Add" : "Update";
        var actor = await Authorize(request.DTO.PermissionRequest, operation, ct);
        var result = await repository.SavePlanPriceAsync(request.Id, request.DTO, actor, ct);
        return ApiResponse<HostPlanPriceResponseDTO>.Success(result, request.Id is null ? "Plan price created successfully." : "Plan price updated successfully.");
    }
}

public sealed class DeleteHostPlanPriceCommandHandler(
    IHostBillingAdministrationRepository repository,
    ICommonRequestService common,
    IStoreProcedureRepository stored)
    : HostBillingHandlerBase(common, stored, BillingConstants.HostBillingPlanPricesModuleCode),
      IRequestHandler<DeleteHostPlanPriceCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(DeleteHostPlanPriceCommand request, CancellationToken ct)
    {
        var actor = await Authorize(request.Permission, "Delete", ct);
        await repository.DeactivatePlanPriceAsync(request.Id, actor, ct);
        return ApiResponse<bool>.Success(true, "Plan price deactivated successfully.");
    }
}

public sealed class GetHostTaxRulesQueryHandler(IHostBillingAdministrationRepository repository, ICommonRequestService common, IStoreProcedureRepository stored)
    : HostBillingHandlerBase(common, stored, BillingConstants.HostBillingTaxRulesModuleCode), IRequestHandler<GetHostTaxRulesQuery, ApiResponse<IReadOnlyList<HostTaxRuleResponseDTO>>>
{
    public async Task<ApiResponse<IReadOnlyList<HostTaxRuleResponseDTO>>> Handle(GetHostTaxRulesQuery request, CancellationToken ct)
    {
        await Authorize(request.Permission, "View", ct);
        return ApiResponse<IReadOnlyList<HostTaxRuleResponseDTO>>.Success(await repository.GetTaxRulesAsync(ct));
    }
}

public sealed class SaveHostTaxRuleCommandHandler(IHostBillingAdministrationRepository repository, ICommonRequestService common, IStoreProcedureRepository stored)
    : HostBillingHandlerBase(common, stored, BillingConstants.HostBillingTaxRulesModuleCode), IRequestHandler<SaveHostTaxRuleCommand, ApiResponse<HostTaxRuleResponseDTO>>
{
    public async Task<ApiResponse<HostTaxRuleResponseDTO>> Handle(SaveHostTaxRuleCommand request, CancellationToken ct)
    {
        var operation = request.Id is null ? "Add" : "Update";
        var actor = await Authorize(request.DTO.PermissionRequest, operation, ct);
        var result = await repository.SaveTaxRuleAsync(request.Id, request.DTO, actor, ct);
        return ApiResponse<HostTaxRuleResponseDTO>.Success(result, request.Id is null ? "Tax rule created successfully." : "Tax rule updated successfully.");
    }
}

public sealed class DeleteHostTaxRuleCommandHandler(IHostBillingAdministrationRepository repository, ICommonRequestService common, IStoreProcedureRepository stored)
    : HostBillingHandlerBase(common, stored, BillingConstants.HostBillingTaxRulesModuleCode), IRequestHandler<DeleteHostTaxRuleCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(DeleteHostTaxRuleCommand request, CancellationToken ct)
    {
        var actor = await Authorize(request.Permission, "Delete", ct);
        await repository.DeactivateTaxRuleAsync(request.Id, actor, ct);
        return ApiResponse<bool>.Success(true, "Tax rule deactivated successfully.");
    }
}

public sealed class GetHostTransactionsQueryHandler(IHostBillingAdministrationRepository repository, ICommonRequestService common, IStoreProcedureRepository stored)
    : HostBillingHandlerBase(common, stored, BillingConstants.HostBillingTransactionsModuleCode), IRequestHandler<GetHostTransactionsQuery, ApiResponse<BillingPageResult<HostPaymentTransactionResponseDTO>>>
{
    public async Task<ApiResponse<BillingPageResult<HostPaymentTransactionResponseDTO>>> Handle(GetHostTransactionsQuery request, CancellationToken ct)
    {
        await Authorize(request.DTO, "View", ct);
        return ApiResponse<BillingPageResult<HostPaymentTransactionResponseDTO>>.Success(await repository.GetTransactionsAsync(request.DTO, ct));
    }
}

public sealed class GetHostRefundsQueryHandler(IHostBillingAdministrationRepository repository, ICommonRequestService common, IStoreProcedureRepository stored)
    : HostBillingHandlerBase(common, stored, BillingConstants.HostBillingRefundsModuleCode), IRequestHandler<GetHostRefundsQuery, ApiResponse<BillingPageResult<HostRefundResponseDTO>>>
{
    public async Task<ApiResponse<BillingPageResult<HostRefundResponseDTO>>> Handle(GetHostRefundsQuery request, CancellationToken ct)
    {
        await Authorize(request.DTO, "View", ct);
        return ApiResponse<BillingPageResult<HostRefundResponseDTO>>.Success(await repository.GetRefundsAsync(request.DTO, ct));
    }
}

public sealed class RequestHostRefundCommandHandler(IHostBillingAdministrationRepository repository, ICommonRequestService common, IStoreProcedureRepository stored)
    : HostBillingHandlerBase(common, stored, BillingConstants.HostBillingRefundsModuleCode), IRequestHandler<RequestHostRefundCommand, ApiResponse<HostRefundResponseDTO>>
{
    public async Task<ApiResponse<HostRefundResponseDTO>> Handle(RequestHostRefundCommand request, CancellationToken ct)
    {
        var actor = await Authorize(request.DTO.PermissionRequest, "Add", ct);
        var result = await repository.RequestRefundAsync(request.DTO, actor, ct);
        return ApiResponse<HostRefundResponseDTO>.Success(result, "Refund request queued successfully.");
    }
}

public sealed class GetHostReconciliationQueryHandler(IHostBillingAdministrationRepository repository, ICommonRequestService common, IStoreProcedureRepository stored)
    : HostBillingHandlerBase(common, stored, BillingConstants.HostBillingReconciliationModuleCode), IRequestHandler<GetHostReconciliationQuery, ApiResponse<HostReconciliationSummaryDTO>>
{
    public async Task<ApiResponse<HostReconciliationSummaryDTO>> Handle(GetHostReconciliationQuery request, CancellationToken ct)
    {
        await Authorize(request.Permission, "View", ct);
        return ApiResponse<HostReconciliationSummaryDTO>.Success(await repository.GetReconciliationAsync(ct));
    }
}

public sealed class RetryHostWebhookCommandHandler(IHostBillingAdministrationRepository repository, ICommonRequestService common, IStoreProcedureRepository stored)
    : HostBillingHandlerBase(common, stored, BillingConstants.HostBillingReconciliationModuleCode), IRequestHandler<RetryHostWebhookCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(RetryHostWebhookCommand request, CancellationToken ct)
    {
        var actor = await Authorize(request.Permission, "Update", ct);
        await repository.RetryWebhookAsync(request.Id, actor, ct);
        return ApiResponse<bool>.Success(true, "Webhook retry queued successfully.");
    }
}

public sealed class GetHostBillingAuditQueryHandler(IHostBillingAdministrationRepository repository, ICommonRequestService common, IStoreProcedureRepository stored)
    : HostBillingHandlerBase(common, stored, BillingConstants.HostBillingAuditModuleCode), IRequestHandler<GetHostBillingAuditQuery, ApiResponse<BillingPageResult<HostBillingAuditResponseDTO>>>
{
    public async Task<ApiResponse<BillingPageResult<HostBillingAuditResponseDTO>>> Handle(GetHostBillingAuditQuery request, CancellationToken ct)
    {
        await Authorize(request.DTO, "View", ct);
        return ApiResponse<BillingPageResult<HostBillingAuditResponseDTO>>.Success(await repository.GetAuditAsync(request.DTO, ct));
    }
}
