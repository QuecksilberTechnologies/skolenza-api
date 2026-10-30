using MediatR;
using skolenza.application.Common.Enums;
using skolenza.application.DTOS.Student;
using skolenza.application.Exceptions;
using skolenza.application.Features.StudentCmd;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Wrappers;

namespace skolenza.application.Features.StudentPortalAuthCmd;

#region Requests

public sealed record GetStudentPortalFeeAdmissionsQuery(long InstitutionId, long AccountId, long StudentId, StudentPortalFeeFilterDTO Filter)
    : IRequest<ApiResponse<List<StudentPortalFeeAdmissionResponseDTO>>>;

public sealed record GetStudentPortalFeeQuoteQuery(long InstitutionId, long AccountId, long StudentId, long AdmissionId)
    : IRequest<ApiResponse<StudentAdmissionFeeQuoteResponseDTO>>;

public sealed record GetStudentPortalFeeBalanceQuery(long InstitutionId, long AccountId, long StudentId, long AdmissionId)
    : IRequest<ApiResponse<StudentAdmissionFeeBalanceResponseDTO>>;

public sealed record GetStudentPortalFeeReceiptsQuery(long InstitutionId, long AccountId, long StudentId, long AdmissionId, StudentPortalFeeFilterDTO Filter)
    : IRequest<ApiResponse<List<StudentFeeReceiptResponseDTO>>>;

public sealed record CreateStudentPortalCashfreeCheckoutCommand(
    long InstitutionId,
    long AccountId,
    long StudentId,
    long AdmissionId,
    long InstallmentId,
    StudentPortalCashfreeCheckoutRequestDTO DTO)
    : IRequest<ApiResponse<StudentCashfreeCheckoutResponseDTO>>;

#endregion

#region Handlers

internal static class StudentPortalFeeAuthorization
{
    public static async Task EnsureGuardianAsync(
        IStudentPortalAuthRepository repository, long institutionId, long accountId, long studentId,
        CancellationToken cancellationToken)
    {
        var profile = await repository.GetProfileAsync(institutionId, accountId, cancellationToken);
        if (profile is null || !profile.Personas.Any(persona =>
            persona.Type == StudentPortalPersonaType.Guardian &&
            persona.LinkedStudents.Any(student => student.StudentId == studentId)))
        {
            throw new NotFoundException("Student fee information was not found.");
        }
    }

    public static async Task EnsureAdmissionAsync(
        IStudentFeeLedgerRepository repository, long institutionId, long studentId, long admissionId,
        CancellationToken cancellationToken)
    {
        if (!await repository.AdmissionBelongsToStudentAsync(institutionId, studentId, admissionId, cancellationToken))
        {
            throw new NotFoundException("Student fee information was not found.");
        }
    }
}

public sealed class GetStudentPortalFeeAdmissionsQueryHandler(IStudentPortalAuthRepository auth, IStudentFeeLedgerRepository fees)
    : IRequestHandler<GetStudentPortalFeeAdmissionsQuery, ApiResponse<List<StudentPortalFeeAdmissionResponseDTO>>>
{
    public async Task<ApiResponse<List<StudentPortalFeeAdmissionResponseDTO>>> Handle(GetStudentPortalFeeAdmissionsQuery request, CancellationToken cancellationToken)
    {
        await StudentPortalFeeAuthorization.EnsureGuardianAsync(auth, request.InstitutionId, request.AccountId, request.StudentId, cancellationToken);
        var page = await fees.GetPortalAdmissionsAsync(request.InstitutionId, request.StudentId, request.Filter, cancellationToken);
        return ApiResponse<List<StudentPortalFeeAdmissionResponseDTO>>.SuccessPaginated(
            page.Data, page.PageNumber, page.PageSize, page.TotalCount, page.TotalPages, "Student fee admissions retrieved successfully.");
    }
}

public sealed class GetStudentPortalFeeQuoteQueryHandler(IStudentPortalAuthRepository auth, IStudentFeeLedgerRepository fees)
    : IRequestHandler<GetStudentPortalFeeQuoteQuery, ApiResponse<StudentAdmissionFeeQuoteResponseDTO>>
{
    public async Task<ApiResponse<StudentAdmissionFeeQuoteResponseDTO>> Handle(GetStudentPortalFeeQuoteQuery request, CancellationToken cancellationToken)
    {
        await StudentPortalFeeAuthorization.EnsureGuardianAsync(auth, request.InstitutionId, request.AccountId, request.StudentId, cancellationToken);
        await StudentPortalFeeAuthorization.EnsureAdmissionAsync(fees, request.InstitutionId, request.StudentId, request.AdmissionId, cancellationToken);
        var quote = await fees.GetAdmissionQuoteAsync(request.AdmissionId, request.InstitutionId, cancellationToken);
        return ApiResponse<StudentAdmissionFeeQuoteResponseDTO>.Success(quote, "Student fee quote retrieved successfully.");
    }
}

public sealed class GetStudentPortalFeeBalanceQueryHandler(IStudentPortalAuthRepository auth, IStudentFeeLedgerRepository fees)
    : IRequestHandler<GetStudentPortalFeeBalanceQuery, ApiResponse<StudentAdmissionFeeBalanceResponseDTO>>
{
    public async Task<ApiResponse<StudentAdmissionFeeBalanceResponseDTO>> Handle(GetStudentPortalFeeBalanceQuery request, CancellationToken cancellationToken)
    {
        await StudentPortalFeeAuthorization.EnsureGuardianAsync(auth, request.InstitutionId, request.AccountId, request.StudentId, cancellationToken);
        await StudentPortalFeeAuthorization.EnsureAdmissionAsync(fees, request.InstitutionId, request.StudentId, request.AdmissionId, cancellationToken);
        var balance = await fees.GetAdmissionBalanceAsync(request.AdmissionId, request.InstitutionId, cancellationToken);
        return ApiResponse<StudentAdmissionFeeBalanceResponseDTO>.Success(balance, "Student fee balance retrieved successfully.");
    }
}

public sealed class GetStudentPortalFeeReceiptsQueryHandler(IStudentPortalAuthRepository auth, IStudentFeeLedgerRepository fees)
    : IRequestHandler<GetStudentPortalFeeReceiptsQuery, ApiResponse<List<StudentFeeReceiptResponseDTO>>>
{
    public async Task<ApiResponse<List<StudentFeeReceiptResponseDTO>>> Handle(GetStudentPortalFeeReceiptsQuery request, CancellationToken cancellationToken)
    {
        await StudentPortalFeeAuthorization.EnsureGuardianAsync(auth, request.InstitutionId, request.AccountId, request.StudentId, cancellationToken);
        await StudentPortalFeeAuthorization.EnsureAdmissionAsync(fees, request.InstitutionId, request.StudentId, request.AdmissionId, cancellationToken);
        var page = await fees.GetReceiptsAsync(request.InstitutionId, new StudentFeeReceiptListRequestDTO
        {
            StudentAdmissionId = request.AdmissionId,
            PageNumber = request.Filter.PageNumber,
            PageSize = request.Filter.PageSize
        }, cancellationToken);
        return ApiResponse<List<StudentFeeReceiptResponseDTO>>.SuccessPaginated(
            page.Data, page.PageNumber, page.PageSize, page.TotalCount, page.TotalPages, "Student fee receipts retrieved successfully.");
    }
}

public sealed class CreateStudentPortalCashfreeCheckoutCommandHandler(
    IStudentPortalAuthRepository auth,
    IStudentFeeLedgerRepository fees,
    IStudentPaymentGatewayConfigurationRepository configuration,
    IStudentGatewaySecretProtector protector,
    IStudentCashfreeClient cashfree)
    : IRequestHandler<CreateStudentPortalCashfreeCheckoutCommand, ApiResponse<StudentCashfreeCheckoutResponseDTO>>
{
    public async Task<ApiResponse<StudentCashfreeCheckoutResponseDTO>> Handle(
        CreateStudentPortalCashfreeCheckoutCommand request,
        CancellationToken cancellationToken)
    {
        await StudentPortalFeeAuthorization.EnsureGuardianAsync(
            auth, request.InstitutionId, request.AccountId, request.StudentId, cancellationToken);
        await StudentPortalFeeAuthorization.EnsureAdmissionAsync(
            fees, request.InstitutionId, request.StudentId, request.AdmissionId, cancellationToken);
        if (!await fees.InstallmentBelongsToAdmissionAsync(
            request.InstitutionId, request.AdmissionId, request.InstallmentId, cancellationToken))
        {
            throw new NotFoundException("Student fee information was not found.");
        }

        return await StudentCashfreeCheckoutOperation.CreateAsync(
            request.InstitutionId, request.InstallmentId, request.DTO.IdempotencyKey,
            fees, configuration, protector, cashfree, cancellationToken,
            token => fees.PreparePortalCashfreeCheckoutAsync(
                request.InstitutionId, request.AccountId, request.StudentId,
                request.AdmissionId, request.InstallmentId, request.DTO.IdempotencyKey, token));
    }
}

#endregion
