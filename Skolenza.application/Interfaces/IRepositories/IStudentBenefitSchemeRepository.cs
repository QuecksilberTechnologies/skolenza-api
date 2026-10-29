using skolenza.application.DTOS.Pagination;
using skolenza.application.DTOS.Student;

namespace skolenza.application.Interfaces.IRepositories;

public interface IStudentBenefitSchemeRepository
{
    Task<PagedResponseDTO<StudentBenefitSchemeResponseDTO>> GetPageAsync(
        long institutionId,
        StudentBenefitSchemeFilterDTO filter,
        CancellationToken cancellationToken);

    Task<StudentBenefitSchemeResponseDTO> SaveAsync(
        long? id,
        long institutionId,
        long actorStaffMemberId,
        StudentBenefitSchemeRequestDTO request,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        long id,
        long institutionId,
        long actorStaffMemberId,
        CancellationToken cancellationToken);
}
