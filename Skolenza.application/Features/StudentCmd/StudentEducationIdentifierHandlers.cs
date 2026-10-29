using MediatR;
using skolenza.application.Common.Enums;
using skolenza.application.Common.Helpers;
using skolenza.application.Constants;
using skolenza.application.DTOS.Student;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Wrappers;

namespace skolenza.application.Features.StudentCmd;

#region Requests

public sealed record GetStudentEducationIdentifiersQuery(StudentEducationIdentifierFilterDTO DTO)
    : IRequest<ApiResponse<List<StudentEducationIdentifierResponseDTO>>>;

public sealed record CreateStudentEducationIdentifierCommand(StudentEducationIdentifierRequestDTO DTO)
    : IRequest<ApiResponse<StudentEducationIdentifierResponseDTO>>;

public sealed record UpdateStudentEducationIdentifierCommand(long Id, StudentEducationIdentifierUpdateDTO DTO)
    : IRequest<ApiResponse<StudentEducationIdentifierResponseDTO>>;

public sealed record SetStudentEducationIdentifierVerificationCommand(
    long Id,
    StudentEducationIdentifierVerificationDTO DTO)
    : IRequest<ApiResponse<StudentEducationIdentifierResponseDTO>>;

#endregion

#region Handlers

public sealed class GetStudentEducationIdentifiersQueryHandler(
    IStudentEducationIdentifierRepository repository,
    ICommonRequestService commonRequestService)
    : IRequestHandler<GetStudentEducationIdentifiersQuery, ApiResponse<List<StudentEducationIdentifierResponseDTO>>>
{
    public async Task<ApiResponse<List<StudentEducationIdentifierResponseDTO>>> Handle(
        GetStudentEducationIdentifiersQuery request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(commonRequestService);
        var result = await repository.GetPageAsync(actor.InstitutionId, request.DTO, cancellationToken);
        return ApiResponse<List<StudentEducationIdentifierResponseDTO>>.SuccessPaginated(
            result.Data,
            result.PageNumber,
            result.PageSize,
            result.TotalCount,
            result.TotalPages,
            "Student education identifiers retrieved successfully.");
    }
}

public sealed class CreateStudentEducationIdentifierCommandHandler(
    IStudentEducationIdentifierRepository repository,
    ICommonRequestService commonRequestService,
    IUnitOfWork unitOfWork,
    IEncryptionService encryptionService)
    : IRequestHandler<CreateStudentEducationIdentifierCommand, ApiResponse<StudentEducationIdentifierResponseDTO>>
{
    public async Task<ApiResponse<StudentEducationIdentifierResponseDTO>> Handle(
        CreateStudentEducationIdentifierCommand request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(commonRequestService);
        var key = await ResolveInstitutionKeyAsync(unitOfWork, actor.InstitutionId, cancellationToken);
        var result = await repository.CreateAsync(
            actor.InstitutionId,
            actor.StaffMemberId,
            request.DTO,
            key,
            cancellationToken);
        return ApiResponse<StudentEducationIdentifierResponseDTO>.Success(
            result,
            "Student education identifier saved securely.");
    }

    internal static async Task<string> ResolveInstitutionKeyAsync(
        IUnitOfWork unitOfWork,
        long institutionId,
        CancellationToken cancellationToken)
    {
        var activeKey = await unitOfWork.InstitutionEncryptionKeyRepository
            .GetActiveKeyByInstitutionIdAsync(institutionId, cancellationToken);
        if (string.IsNullOrWhiteSpace(activeKey?.EncryptionKey))
        {
            throw new NotFoundException("The institution encryption key is unavailable.");
        }

        return activeKey.EncryptionKey;
    }
}

public sealed class UpdateStudentEducationIdentifierCommandHandler(
    IStudentEducationIdentifierRepository repository,
    ICommonRequestService commonRequestService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateStudentEducationIdentifierCommand, ApiResponse<StudentEducationIdentifierResponseDTO>>
{
    public async Task<ApiResponse<StudentEducationIdentifierResponseDTO>> Handle(
        UpdateStudentEducationIdentifierCommand request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(commonRequestService);
        var key = await CreateStudentEducationIdentifierCommandHandler.ResolveInstitutionKeyAsync(
            unitOfWork, actor.InstitutionId, cancellationToken);
        var result = await repository.UpdateAsync(
            actor.InstitutionId,
            actor.StaffMemberId,
            request.Id,
            request.DTO,
            key,
            cancellationToken);
        return ApiResponse<StudentEducationIdentifierResponseDTO>.Success(
            result,
            "Student education identifier updated; verification reset to unverified.");
    }
}

public sealed class SetStudentEducationIdentifierVerificationCommandHandler(
    IStudentEducationIdentifierRepository repository,
    ICommonRequestService commonRequestService)
    : IRequestHandler<SetStudentEducationIdentifierVerificationCommand, ApiResponse<StudentEducationIdentifierResponseDTO>>
{
    public async Task<ApiResponse<StudentEducationIdentifierResponseDTO>> Handle(
        SetStudentEducationIdentifierVerificationCommand request,
        CancellationToken cancellationToken)
    {
        if (request.DTO.VerificationStatus is not ((short)StudentEducationIdentifierVerificationStatus.Verified or
            (short)StudentEducationIdentifierVerificationStatus.Rejected))
        {
            throw new ValidationErrorException("VerificationStatus must be Verified (2) or Rejected (3).");
        }

        if (request.DTO.VerificationStatus == (short)StudentEducationIdentifierVerificationStatus.Rejected &&
            string.IsNullOrWhiteSpace(request.DTO.Reason))
        {
            throw new ValidationErrorException("A reason is required when rejecting an education identifier.");
        }

        var actor = await StudentRequestContext.ResolveActorAsync(commonRequestService);
        var result = await repository.SetVerificationStatusAsync(
            actor.InstitutionId,
            actor.StaffMemberId,
            request.Id,
            request.DTO.VerificationStatus,
            request.DTO.Reason,
            cancellationToken);
        return ApiResponse<StudentEducationIdentifierResponseDTO>.Success(
            result,
            "Student education identifier verification updated successfully.");
    }
}

#endregion
