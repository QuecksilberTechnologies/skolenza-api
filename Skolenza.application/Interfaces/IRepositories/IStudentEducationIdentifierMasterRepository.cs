using skolenza.application.DTOS.Pagination;
using skolenza.application.DTOS.Student;

namespace skolenza.application.Interfaces.IRepositories;

public interface IStudentEducationIdentifierMasterRepository
{
    Task<PagedResponseDTO<StudentEducationIdentifierMasterResponseDTO>> GetPageAsync(
        long institutionId,
        StudentEducationIdentifierMasterFilterDTO filter,
        CancellationToken cancellationToken);

    Task<StudentEducationIdentifierMasterResponseDTO> SaveAsync(
        long? id,
        long institutionId,
        long actorStaffMemberId,
        StudentEducationIdentifierMasterRequestDTO request,
        CancellationToken cancellationToken);

    Task DeleteAsync(long id, long institutionId, long actorStaffMemberId, CancellationToken cancellationToken);
}
