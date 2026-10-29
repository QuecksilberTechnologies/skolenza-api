using System.Security.Cryptography;
using skolenza.application.Common.Enums;
using skolenza.application.DTOS.Student;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IFileStorage;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Wrappers;
using MediatR;

namespace skolenza.application.Features.StudentCmd;

#region Requests

public sealed record UploadStudentAdmissionDocumentCommand(long AdmissionId, StudentAdmissionDocumentUploadDTO DTO)
    : IRequest<ApiResponse<StudentAdmissionDocumentResponseDTO>>;

public sealed record GetStudentAdmissionDocumentsQuery(long AdmissionId, StudentAdmissionDocumentFilterDTO DTO)
    : IRequest<ApiResponse<List<StudentAdmissionDocumentResponseDTO>>>;

public sealed record GetStudentAdmissionDocumentDownloadQuery(long AdmissionId, long DocumentId, StudentDocumentAccessRequestDTO DTO)
    : IRequest<ApiResponse<string>>;

public sealed record ReviewStudentAdmissionDocumentCommand(
    long AdmissionId,
    long DocumentId,
    StudentAdmissionDocumentReviewDTO DTO)
    : IRequest<ApiResponse<StudentAdmissionDocumentResponseDTO>>;

public sealed record ReviewStudentRecordDocumentCommand(
    long StudentId,
    long DocumentId,
    StudentAdmissionDocumentReviewDTO DTO)
    : IRequest<ApiResponse<StudentRecordDocumentResponseDTO>>;

public sealed class StudentDocumentAccessRequestDTO : skolenza.application.DTOs.BaseDTO.PermissionRequestDTO
{
}

#endregion

#region Handlers

public sealed class UploadStudentAdmissionDocumentCommandHandler(
    IStudentAdmissionDocumentRepository repository,
    ICommonRequestService commonRequestService,
    IFileStorageService fileStorageService)
    : IRequestHandler<UploadStudentAdmissionDocumentCommand, ApiResponse<StudentAdmissionDocumentResponseDTO>>
{
    private const long AbsoluteMaximumFileSizeBytes = 104_857_600;

    public async Task<ApiResponse<StudentAdmissionDocumentResponseDTO>> Handle(
        UploadStudentAdmissionDocumentCommand request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(commonRequestService);
        var file = request.DTO.File;
        if (file is null || file.Length <= 0 || file.Length > AbsoluteMaximumFileSizeBytes)
        {
            throw new ValidationErrorException("The file must be between 1 byte and 100 MB.");
        }

        var requirement = await repository.GetUploadRequirementAsync(
            actor.InstitutionId, request.AdmissionId, request.DTO, cancellationToken);
        var contentType = file.ContentType.Split(';', 2)[0].Trim().ToLowerInvariant();
        if (file.Length > requirement.MaxFileSizeBytes || !IsAllowedMimeType(contentType, requirement.AllowedMimeTypes))
        {
            throw new ValidationErrorException("The uploaded file exceeds the requirement's size or allowed MIME type.");
        }

        var originalName = Path.GetFileName(file.FileName);
        if (string.IsNullOrWhiteSpace(originalName) || originalName.Length > 255)
        {
            throw new ValidationErrorException("The original file name is invalid or exceeds 255 characters.");
        }

        string checksum;
        await using (var stream = file.OpenReadStream())
        {
            checksum = Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken)).ToLowerInvariant();
        }

        var storedName = Guid.NewGuid().ToString("N");
        var folderPath = $"institution-{actor.InstitutionId}/student-admissions/{request.AdmissionId}/documents";
        var objectKey = await fileStorageService.UploadFileAsync(file, folderPath, storedName);
        try
        {
            var result = await repository.CreateVersionAsync(
                actor.InstitutionId,
                actor.StaffMemberId,
                request.AdmissionId,
                request.DTO,
                objectKey,
                contentType,
                checksum,
                cancellationToken);
            return ApiResponse<StudentAdmissionDocumentResponseDTO>.Success(
                result, "Admission document uploaded and queued for review.");
        }
        catch
        {
            await fileStorageService.DeleteFileAsync(objectKey);
            throw;
        }
    }

    private static bool IsAllowedMimeType(string contentType, IReadOnlyList<string> allowedMimeTypes)
    {
        return allowedMimeTypes.Any(value =>
        {
            var allowed = value.Trim().ToLowerInvariant();
            return allowed.EndsWith("/*", StringComparison.Ordinal)
                ? contentType.StartsWith(allowed[..^1], StringComparison.Ordinal)
                : string.Equals(contentType, allowed, StringComparison.Ordinal);
        });
    }
}

public sealed class GetStudentAdmissionDocumentsQueryHandler(
    IStudentAdmissionDocumentRepository repository,
    ICommonRequestService commonRequestService)
    : IRequestHandler<GetStudentAdmissionDocumentsQuery, ApiResponse<List<StudentAdmissionDocumentResponseDTO>>>
{
    public async Task<ApiResponse<List<StudentAdmissionDocumentResponseDTO>>> Handle(
        GetStudentAdmissionDocumentsQuery request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(commonRequestService);
        var page = await repository.GetPageAsync(actor.InstitutionId, request.AdmissionId, request.DTO, cancellationToken);
        return ApiResponse<List<StudentAdmissionDocumentResponseDTO>>.SuccessPaginated(
            page.Data, page.PageNumber, page.PageSize, page.TotalCount, page.TotalPages,
            "Admission documents retrieved successfully.");
    }
}

public sealed class GetStudentAdmissionDocumentDownloadQueryHandler(
    IStudentAdmissionDocumentRepository repository,
    ICommonRequestService commonRequestService,
    IFileStorageService fileStorageService)
    : IRequestHandler<GetStudentAdmissionDocumentDownloadQuery, ApiResponse<string>>
{
    public async Task<ApiResponse<string>> Handle(
        GetStudentAdmissionDocumentDownloadQuery request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(commonRequestService);
        var objectKey = await repository.GetDownloadKeyAsync(
            actor.InstitutionId, request.AdmissionId, request.DocumentId, cancellationToken);
        return ApiResponse<string>.Success(
            fileStorageService.GetFileUrl(objectKey, 5),
            "A temporary five-minute document URL was created.");
    }
}

public sealed class ReviewStudentAdmissionDocumentCommandHandler(
    IStudentAdmissionDocumentRepository repository,
    ICommonRequestService commonRequestService)
    : IRequestHandler<ReviewStudentAdmissionDocumentCommand, ApiResponse<StudentAdmissionDocumentResponseDTO>>
{
    public async Task<ApiResponse<StudentAdmissionDocumentResponseDTO>> Handle(
        ReviewStudentAdmissionDocumentCommand request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(commonRequestService);
        var result = await repository.ReviewAsync(
            actor.InstitutionId,
            actor.StaffMemberId,
            request.AdmissionId,
            request.DocumentId,
            request.DTO,
            cancellationToken);
        return ApiResponse<StudentAdmissionDocumentResponseDTO>.Success(result, "Document review decision saved.");
    }
}

public sealed class ReviewStudentRecordDocumentCommandHandler(
    IStudentPortalDocumentRepository repository,
    ICommonRequestService commonRequestService)
    : IRequestHandler<ReviewStudentRecordDocumentCommand, ApiResponse<StudentRecordDocumentResponseDTO>>
{
    public async Task<ApiResponse<StudentRecordDocumentResponseDTO>> Handle(
        ReviewStudentRecordDocumentCommand request,
        CancellationToken cancellationToken)
    {
        var actor = await StudentRequestContext.ResolveActorAsync(commonRequestService);
        var result = await repository.ReviewAsync(
            actor.InstitutionId,
            actor.StaffMemberId,
            request.StudentId,
            request.DocumentId,
            request.DTO,
            cancellationToken);
        return ApiResponse<StudentRecordDocumentResponseDTO>.Success(
            result, "Document review decision saved.");
    }
}

#endregion
