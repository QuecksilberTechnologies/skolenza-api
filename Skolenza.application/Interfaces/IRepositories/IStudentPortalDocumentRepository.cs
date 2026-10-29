using skolenza.application.DTOS.Pagination;
using skolenza.application.DTOS.Student;
using skolenza.application.Common.Enums;

namespace skolenza.application.Interfaces.IRepositories;

public interface IStudentPortalDocumentRepository
{
    Task<IReadOnlyList<StudentPortalDocumentRequirementResponseDTO>> GetRequirementsAsync(
        long institutionId,
        long accountId,
        long studentId,
        StudentDocumentPersonScope personScope,
        CancellationToken cancellationToken);

    Task<StudentPortalDocumentRequirementResponseDTO> GetUploadRequirementAsync(
        long institutionId,
        long accountId,
        long studentId,
        StudentPortalDocumentUploadDTO request,
        CancellationToken cancellationToken);

    Task<StudentRecordDocumentResponseDTO> CreateVersionAsync(
        long institutionId,
        long accountId,
        long studentId,
        StudentPortalDocumentUploadDTO request,
        string objectKey,
        string contentType,
        string checksumSha256,
        CancellationToken cancellationToken);

    Task<PagedResponseDTO<StudentRecordDocumentResponseDTO>> GetPageAsync(
        long institutionId,
        long accountId,
        long studentId,
        StudentPortalDocumentFilterDTO filter,
        CancellationToken cancellationToken);

    Task<string> GetDownloadKeyAsync(
        long institutionId,
        long accountId,
        long studentId,
        long documentId,
        CancellationToken cancellationToken);

    Task<StudentRecordDocumentResponseDTO> ReviewAsync(
        long institutionId,
        long staffMemberId,
        long studentId,
        long documentId,
        StudentAdmissionDocumentReviewDTO request,
        CancellationToken cancellationToken);
}
