using skolenza.application.DTOS.Pagination;
using skolenza.application.DTOS.Student;

namespace skolenza.application.Interfaces.IRepositories;

public interface IStudentDocumentRequirementRepository
{
    Task<PagedResponseDTO<StudentDocumentRequirementResponseDTO>> GetPageAsync(
        long institutionId,
        StudentDocumentRequirementFilterDTO filter,
        CancellationToken cancellationToken);

    Task<StudentDocumentRequirementResponseDTO> CreateAsync(
        long institutionId,
        long actorStaffMemberId,
        StudentDocumentRequirementRequestDTO request,
        CancellationToken cancellationToken);

    Task<StudentDocumentRequirementResponseDTO> UpdateAsync(
        long id,
        long institutionId,
        long actorStaffMemberId,
        StudentDocumentRequirementRequestDTO request,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        long id,
        long institutionId,
        long actorStaffMemberId,
        CancellationToken cancellationToken);
}
