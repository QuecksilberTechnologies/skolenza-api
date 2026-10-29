using skolenza.application.Common.Enums;
using skolenza.application.DTOs.BaseDTO;
using skolenza.application.DTOS.Student;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Wrappers;
using MediatR;

namespace skolenza.application.Features.StudentCmd;

#region Requests

public sealed record GetStudentAdmissionsQuery(StudentAdmissionFilterDTO DTO)
    : IRequest<ApiResponse<List<StudentAdmissionResponseDTO>>>;

public sealed record GetStudentAdmissionQuery(long Id, PermissionRequestDTO DTO)
    : IRequest<ApiResponse<StudentAdmissionResponseDTO>>;

public sealed record GetStudentAdmissionStatusHistoryQuery(
    long AdmissionId,
    StudentAdmissionStatusHistoryFilterDTO DTO)
    : IRequest<ApiResponse<List<StudentAdmissionStatusHistoryResponseDTO>>>;

public sealed record GetStudentsQuery(StudentRecordFilterDTO DTO)
    : IRequest<ApiResponse<List<StudentRecordResponseDTO>>>;

public sealed record GetStudentExitHistoryQuery(long StudentId, StudentExitHistoryFilterDTO DTO)
    : IRequest<ApiResponse<List<StudentExitHistoryResponseDTO>>>;

public sealed record GetStudentOptionsQuery(StudentOptionsRequestDTO DTO)
    : IRequest<ApiResponse<StudentOptionsResponseDTO>>;

public sealed record GetGuardiansQuery(StudentGuardianFilterDTO DTO)
    : IRequest<ApiResponse<List<StudentGuardianProfileResponseDTO>>>;

public sealed record GetStudentGuardiansQuery(long StudentId, StudentGuardianLinksFilterDTO DTO)
    : IRequest<ApiResponse<List<StudentGuardianLinkResponseDTO>>>;

public sealed record GetAdmissionGuardiansQuery(long AdmissionId, StudentGuardianLinksFilterDTO DTO)
    : IRequest<ApiResponse<List<StudentAdmissionGuardianResponseDTO>>>;

public sealed record CreateGuardianCommand(StudentGuardianProfileRequestDTO DTO)
    : IRequest<ApiResponse<StudentGuardianProfileResponseDTO>>;

public sealed record UpdateGuardianCommand(long Id, StudentGuardianProfileRequestDTO DTO)
    : IRequest<ApiResponse<StudentGuardianProfileResponseDTO>>;

public sealed record LinkGuardianCommand(long StudentId, StudentGuardianLinkRequestDTO DTO)
    : IRequest<ApiResponse<StudentGuardianLinkResponseDTO>>;

public sealed record UpdateGuardianLinkCommand(long StudentId, long LinkId, StudentGuardianLinkRequestDTO DTO)
    : IRequest<ApiResponse<StudentGuardianLinkResponseDTO>>;

public sealed record RemoveGuardianLinkCommand(long StudentId, long LinkId, StudentGuardianUnlinkRequestDTO DTO)
    : IRequest<ApiResponse<bool>>;

public sealed record AddAdmissionGuardianCommand(long AdmissionId, StudentAdmissionGuardianRequestDTO DTO)
    : IRequest<ApiResponse<StudentAdmissionGuardianResponseDTO>>;

public sealed record UpdateAdmissionGuardianCommand(long AdmissionId, long LinkId, StudentAdmissionGuardianRequestDTO DTO)
    : IRequest<ApiResponse<StudentAdmissionGuardianResponseDTO>>;

public sealed record RemoveAdmissionGuardianCommand(long AdmissionId, long LinkId, StudentGuardianUnlinkRequestDTO DTO)
    : IRequest<ApiResponse<bool>>;

public sealed record CreateStudentAdmissionCommand(StudentAdmissionRequestDTO DTO)
    : IRequest<ApiResponse<StudentAdmissionResponseDTO>>;

public sealed record UpdateStudentAdmissionCommand(long Id, StudentAdmissionRequestDTO DTO)
    : IRequest<ApiResponse<StudentAdmissionResponseDTO>>;

public sealed record ReviewStudentAdmissionCommand(long Id, StudentAdmissionReviewDTO DTO)
    : IRequest<ApiResponse<StudentAdmissionResponseDTO>>;

public sealed record ExitStudentCommand(long Id, StudentExitRequestDTO DTO)
    : IRequest<ApiResponse<StudentExitResponseDTO>>;

#endregion

#region Handlers

public sealed class GetGuardiansQueryHandler(
    IStudentRegistryRepository repository,
    ICommonRequestService commonRequestService)
    : IRequestHandler<GetGuardiansQuery, ApiResponse<List<StudentGuardianProfileResponseDTO>>>
{
    public async Task<ApiResponse<List<StudentGuardianProfileResponseDTO>>> Handle(
        GetGuardiansQuery request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(commonRequestService);
        var result = await repository.GetGuardiansAsync(actor.InstitutionId, request.DTO, cancellationToken);
        return ApiResponse<List<StudentGuardianProfileResponseDTO>>.SuccessPaginated(
            result.Data, result.PageNumber, result.PageSize, result.TotalCount, result.TotalPages,
            "Guardians retrieved successfully.");
    }
}

public sealed class GetStudentGuardiansQueryHandler(
    IStudentRegistryRepository repository,
    ICommonRequestService commonRequestService)
    : IRequestHandler<GetStudentGuardiansQuery, ApiResponse<List<StudentGuardianLinkResponseDTO>>>
{
    public async Task<ApiResponse<List<StudentGuardianLinkResponseDTO>>> Handle(
        GetStudentGuardiansQuery request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(commonRequestService);
        var result = await repository.GetStudentGuardiansAsync(
            actor.InstitutionId, request.StudentId, request.DTO.IncludeInactive, cancellationToken);
        return ApiResponse<List<StudentGuardianLinkResponseDTO>>.Success(
            result.ToList(), "Student Guardian links retrieved successfully.");
    }
}

public sealed class GetAdmissionGuardiansQueryHandler(
    IStudentRegistryRepository repository,
    ICommonRequestService commonRequestService)
    : IRequestHandler<GetAdmissionGuardiansQuery, ApiResponse<List<StudentAdmissionGuardianResponseDTO>>>
{
    public async Task<ApiResponse<List<StudentAdmissionGuardianResponseDTO>>> Handle(
        GetAdmissionGuardiansQuery request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(commonRequestService);
        var result = await repository.GetAdmissionGuardiansAsync(
            actor.InstitutionId, request.AdmissionId, request.DTO.IncludeInactive, cancellationToken);
        return ApiResponse<List<StudentAdmissionGuardianResponseDTO>>.Success(
            result.ToList(), "Admission Guardian links retrieved successfully.");
    }
}

public sealed class CreateGuardianCommandHandler(
    IStudentRegistryRepository repository,
    ICommonRequestService commonRequestService)
    : IRequestHandler<CreateGuardianCommand, ApiResponse<StudentGuardianProfileResponseDTO>>
{
    public async Task<ApiResponse<StudentGuardianProfileResponseDTO>> Handle(
        CreateGuardianCommand request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(commonRequestService);
        var result = await repository.CreateGuardianAsync(
            actor.InstitutionId, actor.StaffMemberId, request.DTO, cancellationToken);
        return ApiResponse<StudentGuardianProfileResponseDTO>.Success(result, "Guardian profile created successfully.");
    }
}

public sealed class UpdateGuardianCommandHandler(
    IStudentRegistryRepository repository,
    ICommonRequestService commonRequestService)
    : IRequestHandler<UpdateGuardianCommand, ApiResponse<StudentGuardianProfileResponseDTO>>
{
    public async Task<ApiResponse<StudentGuardianProfileResponseDTO>> Handle(
        UpdateGuardianCommand request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(commonRequestService);
        var result = await repository.UpdateGuardianAsync(
            actor.InstitutionId, actor.StaffMemberId, request.Id, request.DTO, cancellationToken);
        return ApiResponse<StudentGuardianProfileResponseDTO>.Success(result, "Guardian profile updated successfully.");
    }
}

public sealed class LinkGuardianCommandHandler(
    IStudentRegistryRepository repository,
    ICommonRequestService commonRequestService)
    : IRequestHandler<LinkGuardianCommand, ApiResponse<StudentGuardianLinkResponseDTO>>
{
    public async Task<ApiResponse<StudentGuardianLinkResponseDTO>> Handle(
        LinkGuardianCommand request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(commonRequestService);
        var result = await repository.LinkGuardianAsync(
            actor.InstitutionId, actor.StaffMemberId, request.StudentId, request.DTO, cancellationToken);
        return ApiResponse<StudentGuardianLinkResponseDTO>.Success(result, "Guardian linked to Student successfully.");
    }
}

public sealed class UpdateGuardianLinkCommandHandler(
    IStudentRegistryRepository repository,
    ICommonRequestService commonRequestService)
    : IRequestHandler<UpdateGuardianLinkCommand, ApiResponse<StudentGuardianLinkResponseDTO>>
{
    public async Task<ApiResponse<StudentGuardianLinkResponseDTO>> Handle(
        UpdateGuardianLinkCommand request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(commonRequestService);
        var result = await repository.UpdateGuardianLinkAsync(
            actor.InstitutionId, actor.StaffMemberId, request.StudentId, request.LinkId, request.DTO, cancellationToken);
        return ApiResponse<StudentGuardianLinkResponseDTO>.Success(result, "Student Guardian link updated successfully.");
    }
}

public sealed class RemoveGuardianLinkCommandHandler(
    IStudentRegistryRepository repository,
    ICommonRequestService commonRequestService)
    : IRequestHandler<RemoveGuardianLinkCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(
        RemoveGuardianLinkCommand request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(commonRequestService);
        await repository.RemoveGuardianLinkAsync(
            actor.InstitutionId, request.StudentId, request.LinkId, request.DTO.EffectiveTo, cancellationToken);
        return ApiResponse<bool>.Success(true, "Guardian link closed successfully; Guardian profile retained.");
    }
}

public sealed class AddAdmissionGuardianCommandHandler(
    IStudentRegistryRepository repository,
    ICommonRequestService commonRequestService)
    : IRequestHandler<AddAdmissionGuardianCommand, ApiResponse<StudentAdmissionGuardianResponseDTO>>
{
    public async Task<ApiResponse<StudentAdmissionGuardianResponseDTO>> Handle(
        AddAdmissionGuardianCommand request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(commonRequestService);
        var result = await repository.AddAdmissionGuardianAsync(
            actor.InstitutionId, actor.StaffMemberId, request.AdmissionId, request.DTO, cancellationToken);
        return ApiResponse<StudentAdmissionGuardianResponseDTO>.Success(
            result, "Guardian added to admission successfully.");
    }
}

public sealed class UpdateAdmissionGuardianCommandHandler(
    IStudentRegistryRepository repository,
    ICommonRequestService commonRequestService)
    : IRequestHandler<UpdateAdmissionGuardianCommand, ApiResponse<StudentAdmissionGuardianResponseDTO>>
{
    public async Task<ApiResponse<StudentAdmissionGuardianResponseDTO>> Handle(
        UpdateAdmissionGuardianCommand request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(commonRequestService);
        var result = await repository.UpdateAdmissionGuardianAsync(
            actor.InstitutionId, actor.StaffMemberId, request.AdmissionId, request.LinkId, request.DTO, cancellationToken);
        return ApiResponse<StudentAdmissionGuardianResponseDTO>.Success(
            result, "Admission Guardian link updated successfully.");
    }
}

public sealed class RemoveAdmissionGuardianCommandHandler(
    IStudentRegistryRepository repository,
    ICommonRequestService commonRequestService)
    : IRequestHandler<RemoveAdmissionGuardianCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(
        RemoveAdmissionGuardianCommand request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(commonRequestService);
        await repository.RemoveAdmissionGuardianAsync(
            actor.InstitutionId, request.AdmissionId, request.LinkId, request.DTO.EffectiveTo, cancellationToken);
        return ApiResponse<bool>.Success(true, "Admission Guardian link closed; Guardian profile retained.");
    }
}

public sealed class GetStudentOptionsQueryHandler
    : IRequestHandler<GetStudentOptionsQuery, ApiResponse<StudentOptionsResponseDTO>>
{
    public Task<ApiResponse<StudentOptionsResponseDTO>> Handle(
        GetStudentOptionsQuery request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var response = new StudentOptionsResponseDTO
        {
            AdmissionStatuses = Values<StudentAdmissionStatus>(),
            LifecycleStatuses = Values<StudentLifecycleStatus>(),
            EnrollmentStatuses = Values<StudentEnrollmentStatus>(),
            DocumentPersonScopes = Values<StudentDocumentPersonScope>(),
            DocumentReviewStatuses = Values<StudentDocumentReviewStatus>(),
            FeeChargeNatures = Values<StudentFeeComponentChargeNature>(),
            FeeInstallmentStatuses = Values<StudentFeeInstallmentStatus>(),
            FeePaymentMethods = Values<StudentFeePaymentMethod>(),
            FeePaymentStatuses = Values<StudentFeePaymentStatus>(),
            FeePaymentAttemptStatuses = Values<StudentFeePaymentAttemptStatus>(),
            FeeWebhookProcessingStatuses = Values<StudentFeeWebhookProcessingStatus>(),
            EducationIdentifierVerificationStatuses = Values<StudentEducationIdentifierVerificationStatus>(),
            ExitReasons = Values<StudentExitReason>(),
            GuardianRelationships = Values<StudentGuardianRelationshipType>(),
            PaymentGatewayEnvironments = Values<StudentPaymentGatewayEnvironment>(),
            PortalDomainVerificationStatuses = Values<StudentPortalDomainVerificationStatus>(),
            PortalPersonaTypes = Values<StudentPortalPersonaType>(),
            PortalOneTimeTokenPurposes = Values<StudentPortalOneTimeTokenPurpose>()
        };
        return Task.FromResult(ApiResponse<StudentOptionsResponseDTO>.Success(
            response,
            "Student workflow options retrieved successfully."));
    }

    private static IReadOnlyList<StudentOptionValueDTO> Values<TEnum>()
        where TEnum : struct, Enum => Enum.GetValues<TEnum>()
        .Select(value => new StudentOptionValueDTO
        {
            Value = Convert.ToInt16(value),
            Name = value.ToString()
        })
        .ToArray();
}

public sealed class GetStudentsQueryHandler(
    IStudentRegistryRepository repository,
    ICommonRequestService commonRequestService)
    : IRequestHandler<GetStudentsQuery, ApiResponse<List<StudentRecordResponseDTO>>>
{
    public async Task<ApiResponse<List<StudentRecordResponseDTO>>> Handle(
        GetStudentsQuery request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(commonRequestService);
        var page = await repository.GetStudentsAsync(actor.InstitutionId, request.DTO, cancellationToken);
        return ApiResponse<List<StudentRecordResponseDTO>>.SuccessPaginated(
            page.Data,
            page.PageNumber,
            page.PageSize,
            page.TotalCount,
            page.TotalPages,
            "Students retrieved successfully.");
    }
}

public sealed class GetStudentExitHistoryQueryHandler(
    IStudentRegistryRepository repository,
    ICommonRequestService commonRequestService)
    : IRequestHandler<GetStudentExitHistoryQuery, ApiResponse<List<StudentExitHistoryResponseDTO>>>
{
    public async Task<ApiResponse<List<StudentExitHistoryResponseDTO>>> Handle(
        GetStudentExitHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(commonRequestService);
        var page = await repository.GetExitHistoryAsync(
            actor.InstitutionId,
            request.StudentId,
            request.DTO,
            cancellationToken);
        return ApiResponse<List<StudentExitHistoryResponseDTO>>.SuccessPaginated(
            page.Data,
            page.PageNumber,
            page.PageSize,
            page.TotalCount,
            page.TotalPages,
            "Student exit history retrieved successfully.");
    }
}

public sealed class GetStudentAdmissionsQueryHandler(
    IStudentRegistryRepository repository,
    ICommonRequestService commonRequestService)
    : IRequestHandler<GetStudentAdmissionsQuery, ApiResponse<List<StudentAdmissionResponseDTO>>>
{
    public async Task<ApiResponse<List<StudentAdmissionResponseDTO>>> Handle(
        GetStudentAdmissionsQuery request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(commonRequestService);
        var page = await repository.GetAdmissionsAsync(actor.InstitutionId, request.DTO, cancellationToken);
        return ApiResponse<List<StudentAdmissionResponseDTO>>.SuccessPaginated(
            page.Data,
            page.PageNumber,
            page.PageSize,
            page.TotalCount,
            page.TotalPages,
            "Student admissions retrieved successfully.");
    }
}

public sealed class GetStudentAdmissionQueryHandler(
    IStudentRegistryRepository repository,
    ICommonRequestService commonRequestService)
    : IRequestHandler<GetStudentAdmissionQuery, ApiResponse<StudentAdmissionResponseDTO>>
{
    public async Task<ApiResponse<StudentAdmissionResponseDTO>> Handle(
        GetStudentAdmissionQuery request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(commonRequestService);
        var admission = await repository.GetAdmissionAsync(actor.InstitutionId, request.Id, cancellationToken)
            ?? throw new NotFoundException("Student admission was not found.");
        return ApiResponse<StudentAdmissionResponseDTO>.Success(admission, "Student admission retrieved successfully.");
    }
}

public sealed class GetStudentAdmissionStatusHistoryQueryHandler(
    IStudentRegistryRepository repository,
    ICommonRequestService commonRequestService)
    : IRequestHandler<GetStudentAdmissionStatusHistoryQuery, ApiResponse<List<StudentAdmissionStatusHistoryResponseDTO>>>
{
    public async Task<ApiResponse<List<StudentAdmissionStatusHistoryResponseDTO>>> Handle(
        GetStudentAdmissionStatusHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(commonRequestService);
        var page = await repository.GetAdmissionStatusHistoryAsync(
            actor.InstitutionId,
            request.AdmissionId,
            request.DTO,
            cancellationToken);
        return ApiResponse<List<StudentAdmissionStatusHistoryResponseDTO>>.SuccessPaginated(
            page.Data,
            page.PageNumber,
            page.PageSize,
            page.TotalCount,
            page.TotalPages,
            "Student admission status history retrieved successfully.");
    }
}

public sealed class CreateStudentAdmissionCommandHandler(
    IStudentRegistryRepository repository,
    ICommonRequestService commonRequestService)
    : IRequestHandler<CreateStudentAdmissionCommand, ApiResponse<StudentAdmissionResponseDTO>>
{
    public async Task<ApiResponse<StudentAdmissionResponseDTO>> Handle(
        CreateStudentAdmissionCommand request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(commonRequestService);
        var response = await repository.CreateAdmissionAsync(
            actor.InstitutionId,
            actor.StaffMemberId,
            request.DTO,
            cancellationToken);
        return ApiResponse<StudentAdmissionResponseDTO>.Success(response, "Student admission draft created successfully.");
    }
}

public sealed class UpdateStudentAdmissionCommandHandler(
    IStudentRegistryRepository repository,
    ICommonRequestService commonRequestService)
    : IRequestHandler<UpdateStudentAdmissionCommand, ApiResponse<StudentAdmissionResponseDTO>>
{
    public async Task<ApiResponse<StudentAdmissionResponseDTO>> Handle(
        UpdateStudentAdmissionCommand request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(commonRequestService);
        var response = await repository.UpdateAdmissionAsync(
            actor.InstitutionId,
            actor.StaffMemberId,
            request.Id,
            request.DTO,
            cancellationToken);
        return ApiResponse<StudentAdmissionResponseDTO>.Success(response, "Student admission draft updated successfully.");
    }
}

public sealed class ReviewStudentAdmissionCommandHandler(
    IStudentRegistryRepository repository,
    ICommonRequestService commonRequestService)
    : IRequestHandler<ReviewStudentAdmissionCommand, ApiResponse<StudentAdmissionResponseDTO>>
{
    public async Task<ApiResponse<StudentAdmissionResponseDTO>> Handle(
        ReviewStudentAdmissionCommand request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(commonRequestService);
        var response = await repository.ReviewAdmissionAsync(
            actor.InstitutionId,
            actor.StaffMemberId,
            request.Id,
            request.DTO,
            cancellationToken);
        return ApiResponse<StudentAdmissionResponseDTO>.Success(response, "Student admission status updated successfully.");
    }
}

public sealed class ExitStudentCommandHandler(
    IStudentRegistryRepository repository,
    ICommonRequestService commonRequestService)
    : IRequestHandler<ExitStudentCommand, ApiResponse<StudentExitResponseDTO>>
{
    public async Task<ApiResponse<StudentExitResponseDTO>> Handle(
        ExitStudentCommand request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(commonRequestService);
        var response = await repository.ExitStudentAsync(
            actor.InstitutionId,
            actor.StaffMemberId,
            request.Id,
            request.DTO,
            cancellationToken);
        return ApiResponse<StudentExitResponseDTO>.Success(response, "Student exit recorded successfully.");
    }
}

#endregion
