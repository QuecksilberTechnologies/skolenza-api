using skolenza.application.DTOS.Pagination;
using skolenza.application.DTOS.Student;

namespace skolenza.application.Interfaces.IRepositories;

public interface IStudentEducationIdentifierRepository
{
    Task<PagedResponseDTO<StudentEducationIdentifierResponseDTO>> GetPageAsync(
        long institutionId,
        StudentEducationIdentifierFilterDTO filter,
        CancellationToken cancellationToken);

    Task<StudentEducationIdentifierResponseDTO> CreateAsync(
        long institutionId,
        long actorStaffMemberId,
        StudentEducationIdentifierRequestDTO request,
        string institutionEncryptionKey,
        CancellationToken cancellationToken);

    Task<StudentEducationIdentifierResponseDTO> UpdateAsync(
        long institutionId,
        long actorStaffMemberId,
        long identifierId,
        StudentEducationIdentifierUpdateDTO request,
        string institutionEncryptionKey,
        CancellationToken cancellationToken);

    Task<StudentEducationIdentifierResponseDTO> SetVerificationStatusAsync(
        long institutionId,
        long actorStaffMemberId,
        long identifierId,
        short verificationStatus,
        string? reason,
        CancellationToken cancellationToken);
}
