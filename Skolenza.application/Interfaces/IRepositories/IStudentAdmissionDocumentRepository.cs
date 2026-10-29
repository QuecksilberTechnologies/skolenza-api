using skolenza.application.DTOS.Pagination;
using skolenza.application.DTOS.Student;

namespace skolenza.application.Interfaces.IRepositories;

public interface IStudentAdmissionDocumentRepository
{
    Task<StudentDocumentRequirementResponseDTO> GetUploadRequirementAsync(
        long institutionId,
        long admissionId,
        StudentAdmissionDocumentUploadDTO request,
        CancellationToken cancellationToken);

    Task<StudentAdmissionDocumentResponseDTO> CreateVersionAsync(
        long institutionId,
        long actorId,
        long admissionId,
        StudentAdmissionDocumentUploadDTO request,
        string objectKey,
        string contentType,
        string checksumSha256,
        CancellationToken cancellationToken);

    Task<PagedResponseDTO<StudentAdmissionDocumentResponseDTO>> GetPageAsync(
        long institutionId,
        long admissionId,
        StudentAdmissionDocumentFilterDTO filter,
        CancellationToken cancellationToken);

    Task<string> GetDownloadKeyAsync(
        long institutionId,
        long admissionId,
        long documentId,
        CancellationToken cancellationToken);

    Task<StudentAdmissionDocumentResponseDTO> ReviewAsync(
        long institutionId,
        long actorId,
        long admissionId,
        long documentId,
        StudentAdmissionDocumentReviewDTO request,
        CancellationToken cancellationToken);
}
