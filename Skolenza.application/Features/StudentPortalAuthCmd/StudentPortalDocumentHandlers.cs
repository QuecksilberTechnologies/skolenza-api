using System.Security.Cryptography;
using MediatR;
using skolenza.application.Common.Enums;
using skolenza.application.Constants;
using skolenza.application.DTOS.Student;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces.IFileStorage;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Wrappers;

namespace skolenza.application.Features.StudentPortalAuthCmd;

#region Requests

public sealed record GetStudentPortalDocumentRequirementsQuery(
    long InstitutionId,
    long PortalAccountId,
    long StudentId,
    StudentDocumentPersonScope PersonScope)
    : IRequest<ApiResponse<List<StudentPortalDocumentRequirementResponseDTO>>>;

public sealed record UploadStudentPortalDocumentCommand(
    long InstitutionId,
    long PortalAccountId,
    long StudentId,
    StudentPortalDocumentUploadDTO DTO)
    : IRequest<ApiResponse<StudentRecordDocumentResponseDTO>>;

public sealed record GetStudentPortalDocumentsQuery(
    long InstitutionId,
    long PortalAccountId,
    long StudentId,
    StudentPortalDocumentFilterDTO DTO)
    : IRequest<ApiResponse<List<StudentRecordDocumentResponseDTO>>>;

public sealed record GetStudentPortalDocumentDownloadQuery(
    long InstitutionId,
    long PortalAccountId,
    long StudentId,
    long DocumentId)
    : IRequest<ApiResponse<string>>;

#endregion

#region Handlers

public sealed class GetStudentPortalDocumentRequirementsQueryHandler(
    IStudentPortalDocumentRepository repository)
    : IRequestHandler<GetStudentPortalDocumentRequirementsQuery,
        ApiResponse<List<StudentPortalDocumentRequirementResponseDTO>>>
{
    public async Task<ApiResponse<List<StudentPortalDocumentRequirementResponseDTO>>> Handle(
        GetStudentPortalDocumentRequirementsQuery request,
        CancellationToken cancellationToken)
    {
        var result = await repository.GetRequirementsAsync(
            request.InstitutionId,
            request.PortalAccountId,
            request.StudentId,
            request.PersonScope,
            cancellationToken);

        return ApiResponse<List<StudentPortalDocumentRequirementResponseDTO>>.Success(
            result.ToList(), "Document requirements retrieved successfully.");
    }
}

public sealed class UploadStudentPortalDocumentCommandHandler(
    IStudentPortalDocumentRepository repository,
    IFileStorageService fileStorageService)
    : IRequestHandler<UploadStudentPortalDocumentCommand, ApiResponse<StudentRecordDocumentResponseDTO>>
{
    public async Task<ApiResponse<StudentRecordDocumentResponseDTO>> Handle(
        UploadStudentPortalDocumentCommand request,
        CancellationToken cancellationToken)
    {
        var file = request.DTO.File;
        if (file is null || file.Length <= 0 || file.Length > StudentConstants.PortalDocumentAbsoluteMaximumFileSizeBytes)
        {
            throw new ValidationErrorException("The file must be between 1 byte and 100 MB.");
        }

        var requirement = await repository.GetUploadRequirementAsync(
            request.InstitutionId,
            request.PortalAccountId,
            request.StudentId,
            request.DTO,
            cancellationToken);
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

        var objectKey = await fileStorageService.UploadFileAsync(
            file,
            $"institution-{request.InstitutionId}/students/{request.StudentId}/documents",
            Guid.NewGuid().ToString("N"));
        try
        {
            var result = await repository.CreateVersionAsync(
                request.InstitutionId,
                request.PortalAccountId,
                request.StudentId,
                request.DTO,
                objectKey,
                contentType,
                checksum,
                cancellationToken);
            return ApiResponse<StudentRecordDocumentResponseDTO>.Success(
                result, "Student document uploaded and queued for review.");
        }
        catch
        {
            await fileStorageService.DeleteFileAsync(objectKey);
            throw;
        }
    }

    private static bool IsAllowedMimeType(string contentType, IReadOnlyList<string> allowedMimeTypes) =>
        allowedMimeTypes.Any(value =>
        {
            var allowed = value.Trim().ToLowerInvariant();
            return allowed.EndsWith("/*", StringComparison.Ordinal)
                ? contentType.StartsWith(allowed[..^1], StringComparison.Ordinal)
                : string.Equals(contentType, allowed, StringComparison.Ordinal);
        });
}

public sealed class GetStudentPortalDocumentsQueryHandler(
    IStudentPortalDocumentRepository repository)
    : IRequestHandler<GetStudentPortalDocumentsQuery, ApiResponse<List<StudentRecordDocumentResponseDTO>>>
{
    public async Task<ApiResponse<List<StudentRecordDocumentResponseDTO>>> Handle(
        GetStudentPortalDocumentsQuery request,
        CancellationToken cancellationToken)
    {
        var page = await repository.GetPageAsync(
            request.InstitutionId,
            request.PortalAccountId,
            request.StudentId,
            request.DTO,
            cancellationToken);
        return ApiResponse<List<StudentRecordDocumentResponseDTO>>.SuccessPaginated(
            page.Data, page.PageNumber, page.PageSize, page.TotalCount, page.TotalPages,
            "Student documents retrieved successfully.");
    }
}

public sealed class GetStudentPortalDocumentDownloadQueryHandler(
    IStudentPortalDocumentRepository repository,
    IFileStorageService fileStorageService)
    : IRequestHandler<GetStudentPortalDocumentDownloadQuery, ApiResponse<string>>
{
    public async Task<ApiResponse<string>> Handle(
        GetStudentPortalDocumentDownloadQuery request,
        CancellationToken cancellationToken)
    {
        var objectKey = await repository.GetDownloadKeyAsync(
            request.InstitutionId,
            request.PortalAccountId,
            request.StudentId,
            request.DocumentId,
            cancellationToken);
        return ApiResponse<string>.Success(
            fileStorageService.GetFileUrl(objectKey, 5),
            "A temporary five-minute document URL was created.");
    }
}

#endregion
