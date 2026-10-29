using skolenza.application.DTOS.Pagination;
using skolenza.application.DTOS.Student;

namespace skolenza.application.Interfaces.IRepositories;

public interface IStudentRegistryRepository
{
    Task<PagedResponseDTO<StudentRecordResponseDTO>> GetStudentsAsync(
        long institutionId,
        StudentRecordFilterDTO filter,
        CancellationToken cancellationToken);

    Task<PagedResponseDTO<StudentAdmissionResponseDTO>> GetAdmissionsAsync(
        long institutionId,
        StudentAdmissionFilterDTO filter,
        CancellationToken cancellationToken);

    Task<StudentAdmissionResponseDTO?> GetAdmissionAsync(
        long institutionId,
        long admissionId,
        CancellationToken cancellationToken);

    Task<PagedResponseDTO<StudentAdmissionStatusHistoryResponseDTO>> GetAdmissionStatusHistoryAsync(
        long institutionId,
        long admissionId,
        StudentAdmissionStatusHistoryFilterDTO filter,
        CancellationToken cancellationToken);

    Task<StudentAdmissionResponseDTO> CreateAdmissionAsync(
        long institutionId,
        long actorId,
        StudentAdmissionRequestDTO request,
        CancellationToken cancellationToken);

    Task<StudentAdmissionResponseDTO> UpdateAdmissionAsync(
        long institutionId,
        long actorId,
        long admissionId,
        StudentAdmissionRequestDTO request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<StudentAdmissionGuardianResponseDTO>> GetAdmissionGuardiansAsync(
        long institutionId,
        long admissionId,
        bool includeInactive,
        CancellationToken cancellationToken);

    Task<StudentAdmissionGuardianResponseDTO> AddAdmissionGuardianAsync(
        long institutionId,
        long actorId,
        long admissionId,
        StudentAdmissionGuardianRequestDTO request,
        CancellationToken cancellationToken);

    Task<StudentAdmissionGuardianResponseDTO> UpdateAdmissionGuardianAsync(
        long institutionId,
        long actorId,
        long admissionId,
        long linkId,
        StudentAdmissionGuardianRequestDTO request,
        CancellationToken cancellationToken);

    Task RemoveAdmissionGuardianAsync(
        long institutionId,
        long admissionId,
        long linkId,
        DateOnly effectiveTo,
        CancellationToken cancellationToken);

    Task<StudentAdmissionResponseDTO> ReviewAdmissionAsync(
        long institutionId,
        long actorId,
        long admissionId,
        StudentAdmissionReviewDTO request,
        CancellationToken cancellationToken);

    Task<StudentExitResponseDTO> ExitStudentAsync(
        long institutionId,
        long actorId,
        long studentId,
        StudentExitRequestDTO request,
        CancellationToken cancellationToken);

    Task<PagedResponseDTO<StudentExitHistoryResponseDTO>> GetExitHistoryAsync(
        long institutionId,
        long studentId,
        StudentExitHistoryFilterDTO filter,
        CancellationToken cancellationToken);

    Task<PagedResponseDTO<StudentGuardianProfileResponseDTO>> GetGuardiansAsync(
        long institutionId,
        StudentGuardianFilterDTO filter,
        CancellationToken cancellationToken);

    Task<StudentGuardianProfileResponseDTO> CreateGuardianAsync(
        long institutionId,
        long actorId,
        StudentGuardianProfileRequestDTO request,
        CancellationToken cancellationToken);

    Task<StudentGuardianProfileResponseDTO> UpdateGuardianAsync(
        long institutionId,
        long actorId,
        long guardianId,
        StudentGuardianProfileRequestDTO request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<StudentGuardianLinkResponseDTO>> GetStudentGuardiansAsync(
        long institutionId,
        long studentId,
        bool includeInactive,
        CancellationToken cancellationToken);

    Task<StudentGuardianLinkResponseDTO> LinkGuardianAsync(
        long institutionId,
        long actorId,
        long studentId,
        StudentGuardianLinkRequestDTO request,
        CancellationToken cancellationToken);

    Task<StudentGuardianLinkResponseDTO> UpdateGuardianLinkAsync(
        long institutionId,
        long actorId,
        long studentId,
        long linkId,
        StudentGuardianLinkRequestDTO request,
        CancellationToken cancellationToken);

    Task RemoveGuardianLinkAsync(
        long institutionId,
        long studentId,
        long linkId,
        DateOnly effectiveTo,
        CancellationToken cancellationToken);
}
