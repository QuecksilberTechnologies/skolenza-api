using skolenza.application.Common.Helpers;
using skolenza.application.Common.Enums;
using skolenza.application.Constants;
using skolenza.application.DTOS.Student;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;

namespace skolenza.application.Features.StudentCmd;

#region Requests

public sealed record GetStudentFeeStructuresQuery(StudentFeeStructureFilterDTO DTO)
    : IRequest<ApiResponse<List<StudentFeeStructureResponseDTO>>>;

public sealed record CreateStudentFeeStructureCommand(StudentFeeStructureRequestDTO DTO)
    : IRequest<ApiResponse<StudentFeeStructureResponseDTO>>;

public sealed record UpdateStudentFeeStructureCommand(long Id, StudentFeeStructureRequestDTO DTO)
    : IRequest<ApiResponse<StudentFeeStructureResponseDTO>>;

public sealed record DeactivateStudentFeeStructureCommand(long Id, StudentFeeMasterDeleteDTO DTO)
    : IRequest<ApiResponse<bool>>;

public sealed record CreateStudentAdmissionFeeQuoteCommand(long AdmissionId, StudentAdmissionFeeQuoteRequestDTO DTO)
    : IRequest<ApiResponse<StudentAdmissionFeeQuoteResponseDTO>>;

public sealed record GetStudentFeeQuotesQuery(StudentFeeQuoteListRequestDTO DTO)
    : IRequest<ApiResponse<List<StudentAdmissionFeeQuoteResponseDTO>>>;

public sealed record GetStudentAdmissionFeeBalanceQuery(long AdmissionId, StudentAdmissionFeeBalanceRequestDTO DTO)
    : IRequest<ApiResponse<StudentAdmissionFeeBalanceResponseDTO>>;

public sealed record GetStudentFeeReceiptsQuery(StudentFeeReceiptListRequestDTO DTO)
    : IRequest<ApiResponse<List<StudentFeeReceiptResponseDTO>>>;

public sealed record RecordStudentOfflineFeePaymentCommand(long InstallmentId, StudentOfflinePaymentRequestDTO DTO)
    : IRequest<ApiResponse<StudentFeePaymentResponseDTO>>;

public sealed record GetStudentPaymentGatewayConfigurationQuery(StudentPaymentGatewayConfigurationFilterDTO DTO)
    : IRequest<ApiResponse<StudentPaymentGatewayConfigurationResponseDTO>>;

public sealed record CreateStudentPaymentGatewayConfigurationCommand(StudentPaymentGatewayConfigurationRequestDTO DTO)
    : IRequest<ApiResponse<StudentPaymentGatewayConfigurationResponseDTO>>;

public sealed record UpdateStudentPaymentGatewayConfigurationCommand(StudentPaymentGatewayConfigurationRequestDTO DTO)
    : IRequest<ApiResponse<StudentPaymentGatewayConfigurationResponseDTO>>;

public sealed record CreateStudentCashfreeCheckoutCommand(
    long InstallmentId,
    StudentCashfreeCheckoutRequestDTO DTO)
    : IRequest<ApiResponse<StudentCashfreeCheckoutResponseDTO>>;

public sealed record ReconcileStudentCashfreePaymentCommand(
    long AttemptId,
    StudentFeePaymentStatusRequestDTO DTO)
    : IRequest<ApiResponse<StudentCashfreePaymentStatusResponseDTO>>;

#endregion

#region Handlers

public sealed class GetStudentFeeStructuresQueryHandler(
    IStudentFeeLedgerRepository repository,
    ICommonRequestService common)
    : IRequestHandler<GetStudentFeeStructuresQuery, ApiResponse<List<StudentFeeStructureResponseDTO>>>
{
    public async Task<ApiResponse<List<StudentFeeStructureResponseDTO>>> Handle(
        GetStudentFeeStructuresQuery request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(common);
        var page = await repository.GetStructuresAsync(actor.InstitutionId, request.DTO, cancellationToken);
        return ApiResponse<List<StudentFeeStructureResponseDTO>>.SuccessPaginated(
            page.Data, page.PageNumber, page.PageSize, page.TotalCount, page.TotalPages,
            "Student fee structures retrieved successfully.");
    }
}

public sealed class CreateStudentFeeStructureCommandHandler(
    IStudentFeeLedgerRepository repository,
    ICommonRequestService common)
    : IRequestHandler<CreateStudentFeeStructureCommand, ApiResponse<StudentFeeStructureResponseDTO>>
{
    public async Task<ApiResponse<StudentFeeStructureResponseDTO>> Handle(
        CreateStudentFeeStructureCommand request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(common);
        var result = await repository.SaveStructureAsync(
            null, actor.InstitutionId, actor.StaffMemberId, request.DTO, cancellationToken);
        return ApiResponse<StudentFeeStructureResponseDTO>.Success(result, "Student fee structure created successfully.");
    }
}

public sealed class UpdateStudentFeeStructureCommandHandler(
    IStudentFeeLedgerRepository repository,
    ICommonRequestService common)
    : IRequestHandler<UpdateStudentFeeStructureCommand, ApiResponse<StudentFeeStructureResponseDTO>>
{
    public async Task<ApiResponse<StudentFeeStructureResponseDTO>> Handle(
        UpdateStudentFeeStructureCommand request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(common);
        var result = await repository.SaveStructureAsync(
            request.Id, actor.InstitutionId, actor.StaffMemberId, request.DTO, cancellationToken);
        return ApiResponse<StudentFeeStructureResponseDTO>.Success(result, "Student fee structure updated successfully.");
    }
}

public sealed class DeactivateStudentFeeStructureCommandHandler(
    IStudentFeeLedgerRepository repository,
    ICommonRequestService common)
    : IRequestHandler<DeactivateStudentFeeStructureCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(
        DeactivateStudentFeeStructureCommand request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(common);
        await repository.DeactivateStructureAsync(request.Id, actor.InstitutionId, actor.StaffMemberId, cancellationToken);
        return ApiResponse<bool>.Success(true, "Student fee structure deactivated successfully.");
    }
}

public sealed class CreateStudentAdmissionFeeQuoteCommandHandler(
    IStudentFeeLedgerRepository repository,
    ICommonRequestService common)
    : IRequestHandler<CreateStudentAdmissionFeeQuoteCommand, ApiResponse<StudentAdmissionFeeQuoteResponseDTO>>
{
    public async Task<ApiResponse<StudentAdmissionFeeQuoteResponseDTO>> Handle(
        CreateStudentAdmissionFeeQuoteCommand request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(common);
        var result = await repository.CreateQuoteAsync(
            request.AdmissionId, actor.InstitutionId, actor.StaffMemberId, request.DTO, cancellationToken);
        return ApiResponse<StudentAdmissionFeeQuoteResponseDTO>.Success(result, "Admission fee quote created successfully.");
    }
}

public sealed class GetStudentFeeQuotesQueryHandler(
    IStudentFeeLedgerRepository repository,
    ICommonRequestService common)
    : IRequestHandler<GetStudentFeeQuotesQuery, ApiResponse<List<StudentAdmissionFeeQuoteResponseDTO>>>
{
    public async Task<ApiResponse<List<StudentAdmissionFeeQuoteResponseDTO>>> Handle(
        GetStudentFeeQuotesQuery request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(common);
        var page = await repository.GetQuotesAsync(actor.InstitutionId, request.DTO, cancellationToken);
        return ApiResponse<List<StudentAdmissionFeeQuoteResponseDTO>>.SuccessPaginated(
            page.Data, page.PageNumber, page.PageSize, page.TotalCount, page.TotalPages,
            "Admission fee quotes retrieved successfully.");
    }
}

public sealed class GetStudentAdmissionFeeBalanceQueryHandler(
    IStudentFeeLedgerRepository repository,
    ICommonRequestService common)
    : IRequestHandler<GetStudentAdmissionFeeBalanceQuery, ApiResponse<StudentAdmissionFeeBalanceResponseDTO>>
{
    public async Task<ApiResponse<StudentAdmissionFeeBalanceResponseDTO>> Handle(
        GetStudentAdmissionFeeBalanceQuery request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(common);
        var result = await repository.GetAdmissionBalanceAsync(request.AdmissionId, actor.InstitutionId, cancellationToken);
        return ApiResponse<StudentAdmissionFeeBalanceResponseDTO>.Success(result, "Admission fee balance retrieved successfully.");
    }
}

public sealed class GetStudentFeeReceiptsQueryHandler(
    IStudentFeeLedgerRepository repository,
    ICommonRequestService common)
    : IRequestHandler<GetStudentFeeReceiptsQuery, ApiResponse<List<StudentFeeReceiptResponseDTO>>>
{
    public async Task<ApiResponse<List<StudentFeeReceiptResponseDTO>>> Handle(
        GetStudentFeeReceiptsQuery request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(common);
        var page = await repository.GetReceiptsAsync(actor.InstitutionId, request.DTO, cancellationToken);
        return ApiResponse<List<StudentFeeReceiptResponseDTO>>.SuccessPaginated(
            page.Data, page.PageNumber, page.PageSize, page.TotalCount, page.TotalPages,
            "Student fee receipts retrieved successfully.");
    }
}

public sealed class RecordStudentOfflineFeePaymentCommandHandler(
    IStudentFeeLedgerRepository repository,
    ICommonRequestService common)
    : IRequestHandler<RecordStudentOfflineFeePaymentCommand, ApiResponse<StudentFeePaymentResponseDTO>>
{
    public async Task<ApiResponse<StudentFeePaymentResponseDTO>> Handle(
        RecordStudentOfflineFeePaymentCommand request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(common);
        var result = await repository.RecordOfflinePaymentAsync(
            request.InstallmentId, actor.InstitutionId, actor.StaffMemberId, request.DTO, cancellationToken);
        return ApiResponse<StudentFeePaymentResponseDTO>.Success(result, "Offline student fee payment recorded successfully.");
    }
}

public sealed class GetStudentPaymentGatewayConfigurationQueryHandler(
    IStudentPaymentGatewayConfigurationRepository repository,
    ICommonRequestService common)
    : IRequestHandler<GetStudentPaymentGatewayConfigurationQuery, ApiResponse<StudentPaymentGatewayConfigurationResponseDTO>>
{
    public async Task<ApiResponse<StudentPaymentGatewayConfigurationResponseDTO>> Handle(
        GetStudentPaymentGatewayConfigurationQuery request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(common);
        var configuration = await repository.GetAsync(actor.InstitutionId, cancellationToken);
        if (configuration is null)
        {
            throw new skolenza.application.Exceptions.NotFoundException("Cashfree tuition gateway configuration was not found.");
        }

        return ApiResponse<StudentPaymentGatewayConfigurationResponseDTO>.Success(
            configuration, "Student payment gateway configuration retrieved successfully.");
    }
}

public sealed class CreateStudentPaymentGatewayConfigurationCommandHandler(
    IStudentPaymentGatewayConfigurationRepository repository,
    IStudentGatewaySecretProtector secretProtector,
    ICommonRequestService common)
    : IRequestHandler<CreateStudentPaymentGatewayConfigurationCommand, ApiResponse<StudentPaymentGatewayConfigurationResponseDTO>>
{
    public async Task<ApiResponse<StudentPaymentGatewayConfigurationResponseDTO>> Handle(
        CreateStudentPaymentGatewayConfigurationCommand request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(common);
        var entity = await StudentPaymentGatewayConfigurationFactory.BuildAsync(
            request.DTO, actor.InstitutionId, actor.StaffMemberId, secretProtector, cancellationToken);
        var configuration = await repository.CreateAsync(entity, cancellationToken);
        return ApiResponse<StudentPaymentGatewayConfigurationResponseDTO>.Success(
            configuration, "Cashfree tuition gateway configuration created successfully.");
    }
}

public sealed class UpdateStudentPaymentGatewayConfigurationCommandHandler(
    IStudentPaymentGatewayConfigurationRepository repository,
    IStudentGatewaySecretProtector secretProtector,
    ICommonRequestService common)
    : IRequestHandler<UpdateStudentPaymentGatewayConfigurationCommand, ApiResponse<StudentPaymentGatewayConfigurationResponseDTO>>
{
    public async Task<ApiResponse<StudentPaymentGatewayConfigurationResponseDTO>> Handle(
        UpdateStudentPaymentGatewayConfigurationCommand request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(common);
        var entity = await StudentPaymentGatewayConfigurationFactory.BuildAsync(
            request.DTO, actor.InstitutionId, actor.StaffMemberId, secretProtector, cancellationToken);
        var configuration = await repository.UpdateAsync(entity, request.DTO.Version, cancellationToken);
        return ApiResponse<StudentPaymentGatewayConfigurationResponseDTO>.Success(
            configuration, "Cashfree tuition gateway configuration updated successfully.");
    }
}

public sealed class CreateStudentCashfreeCheckoutCommandHandler(
    IStudentFeeLedgerRepository feeRepository,
    IStudentPaymentGatewayConfigurationRepository configurationRepository,
    IStudentGatewaySecretProtector secretProtector,
    IStudentCashfreeClient cashfreeClient,
    ICommonRequestService common)
    : IRequestHandler<CreateStudentCashfreeCheckoutCommand, ApiResponse<StudentCashfreeCheckoutResponseDTO>>
{
    public async Task<ApiResponse<StudentCashfreeCheckoutResponseDTO>> Handle(
        CreateStudentCashfreeCheckoutCommand request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(common);
        var configuration = await configurationRepository.GetCredentialsAsync(actor.InstitutionId, cancellationToken)
            ?? throw new skolenza.application.Exceptions.NotFoundException("An active Cashfree tuition gateway configuration is required.");
        if (!Enum.IsDefined((StudentPaymentGatewayEnvironment)configuration.Environment))
        {
            throw new skolenza.application.Exceptions.ValidationErrorException("The configured Cashfree environment is invalid.");
        }

        var checkout = await feeRepository.PrepareCashfreeCheckoutAsync(
            request.InstallmentId, actor.InstitutionId, request.DTO.IdempotencyKey, cancellationToken);
        if (!string.IsNullOrWhiteSpace(checkout.PaymentSessionId) && checkout.ExpiresAt.HasValue)
        {
            return ApiResponse<StudentCashfreeCheckoutResponseDTO>.Success(new StudentCashfreeCheckoutResponseDTO
            {
                PaymentAttemptId = checkout.PaymentAttemptId,
                GatewayOrderId = checkout.GatewayOrderId,
                PaymentSessionId = checkout.PaymentSessionId,
                CurrencyCode = checkout.CurrencyCode,
                Amount = checkout.Amount,
                ExpiresAt = checkout.ExpiresAt.Value
            }, "Cashfree checkout session retrieved successfully.");
        }

        var clientSecret = await secretProtector.UnprotectAsync(
            actor.InstitutionId, configuration.ProtectedClientSecret, cancellationToken);
        var providerOrder = await cashfreeClient.CreateOrderAsync(
            (StudentPaymentGatewayEnvironment)configuration.Environment,
            configuration.ClientId,
            clientSecret,
            checkout,
            CreateProviderIdempotencyKey(checkout.InstitutionId, checkout.PaymentAttemptId),
            cancellationToken);
        var response = await feeRepository.SaveCashfreeOrderAsync(
            checkout.PaymentAttemptId,
            actor.InstitutionId,
            providerOrder.PaymentSessionId,
            providerOrder.ExpiresAt ?? DateTimeOffset.UtcNow.AddMinutes(15),
            cancellationToken);
        return ApiResponse<StudentCashfreeCheckoutResponseDTO>.Success(response, "Cashfree checkout session created successfully.");
    }

    private static Guid CreateProviderIdempotencyKey(long institutionId, long attemptId)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes($"skolenza-student-cashfree:{institutionId}:{attemptId}"));
        return new Guid(bytes.AsSpan(0, 16));
    }
}

public sealed class ReconcileStudentCashfreePaymentCommandHandler(
    IStudentFeeLedgerRepository feeRepository,
    IStudentPaymentGatewayConfigurationRepository configurationRepository,
    IStudentGatewaySecretProtector secretProtector,
    IStudentCashfreeClient cashfreeClient,
    ICommonRequestService common)
    : IRequestHandler<ReconcileStudentCashfreePaymentCommand, ApiResponse<StudentCashfreePaymentStatusResponseDTO>>
{
    public async Task<ApiResponse<StudentCashfreePaymentStatusResponseDTO>> Handle(
        ReconcileStudentCashfreePaymentCommand request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(common);
        var checkout = await feeRepository.GetCashfreeAttemptAsync(
            request.AttemptId, actor.InstitutionId, cancellationToken);
        if (checkout.CurrencyDecimalPlaces == 3)
        {
            throw new skolenza.application.Exceptions.ConflictException(
                "Cashfree reconciliation is not available for a 3-decimal currency attempt.");
        }

        var configuration = await configurationRepository.GetCredentialsAsync(actor.InstitutionId, cancellationToken)
            ?? throw new skolenza.application.Exceptions.NotFoundException("An active Cashfree tuition gateway configuration is required.");
        var clientSecret = await secretProtector.UnprotectAsync(
            actor.InstitutionId, configuration.ProtectedClientSecret, cancellationToken);
        var payments = await cashfreeClient.GetPaymentsAsync(
            (StudentPaymentGatewayEnvironment)configuration.Environment,
            configuration.ClientId,
            clientSecret,
            checkout.GatewayOrderId,
            cancellationToken);
        var payment = payments.OrderByDescending(item => item.PaidAt).FirstOrDefault();
        if (payment is null)
        {
            throw new skolenza.application.Exceptions.NotFoundException("Cashfree has not returned a payment attempt for this order yet.");
        }

        var status = await feeRepository.RecordCashfreePaymentAsync(
            checkout.PaymentAttemptId, payment, null, null, null, cancellationToken);
        return ApiResponse<StudentCashfreePaymentStatusResponseDTO>.Success(status, "Cashfree payment status reconciled successfully.");
    }
}

internal static class StudentPaymentGatewayConfigurationFactory
{
    public static async Task<StudentPaymentGatewayConfiguration> BuildAsync(
        StudentPaymentGatewayConfigurationRequestDTO request,
        long institutionId,
        long actorId,
        IStudentGatewaySecretProtector secretProtector,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(request.Environment))
        {
            throw new skolenza.application.Exceptions.ValidationErrorException("The selected Cashfree environment is invalid.");
        }

        return new StudentPaymentGatewayConfiguration
        {
            InstitutionId = institutionId,
            GatewayCode = StudentConstants.CashfreeGatewayCode,
            Environment = (short)request.Environment,
            ClientId = request.ClientId.Trim(),
            ProtectedClientSecret = await secretProtector.ProtectAsync(institutionId, request.ClientSecret, cancellationToken),
            ProtectedWebhookSecret = await secretProtector.ProtectAsync(institutionId, request.WebhookSecret, cancellationToken),
            IsActive = request.IsActive,
            Version = request.Version,
            CreatedById = actorId,
            CreatedDateTime = DateTime.UtcNow,
            UpdatedById = actorId,
            UpdatedDateTime = DateTime.UtcNow
        };
    }
}

#endregion
