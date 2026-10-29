using skolenza.application.DTOS.Pagination;
using skolenza.application.DTOS.Student;

namespace skolenza.application.Interfaces.IRepositories;

public interface IStudentFeeMasterRepository
{
    Task<PagedResponseDTO<StudentFeeFrequencyResponseDTO>> GetFrequenciesAsync(
        long institutionId,
        StudentFeeFrequencyFilterDTO filter,
        CancellationToken cancellationToken);

    Task<StudentFeeFrequencyResponseDTO> SaveFrequencyAsync(
        long? id,
        long institutionId,
        long actorStaffMemberId,
        StudentFeeFrequencyRequestDTO request,
        CancellationToken cancellationToken);

    Task DeleteFrequencyAsync(
        long id,
        long institutionId,
        long actorStaffMemberId,
        CancellationToken cancellationToken);

    Task<PagedResponseDTO<StudentFeeComponentResponseDTO>> GetComponentsAsync(
        long institutionId,
        StudentFeeComponentFilterDTO filter,
        CancellationToken cancellationToken);

    Task<StudentFeeComponentResponseDTO> SaveComponentAsync(
        long? id,
        long institutionId,
        long actorStaffMemberId,
        StudentFeeComponentRequestDTO request,
        CancellationToken cancellationToken);

    Task DeleteComponentAsync(
        long id,
        long institutionId,
        long actorStaffMemberId,
        CancellationToken cancellationToken);
}
