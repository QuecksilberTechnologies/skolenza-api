using skolenza.application.DTOS.Staff;

namespace skolenza.application.Interfaces.IRepositories;

public interface IStaffAcademicStructureRepository
{
    Task<List<TeachingAssignmentTypeOptionDTO>> GetTeachingAssignmentTypesAsync(CancellationToken cancellationToken = default);
    Task<List<AcademicMasterResponseDTO>> GetMastersAsync(AcademicMasterType type, long institutionId, bool activeOnly, CancellationToken cancellationToken = default);
    Task<AcademicMasterResponseDTO> SaveMasterAsync(AcademicMasterType type, AcademicMasterRequestDTO request, long institutionId, long actorStaffMemberId, CancellationToken cancellationToken = default);
    Task DeleteMasterAsync(AcademicMasterType type, long id, long institutionId, long actorStaffMemberId, CancellationToken cancellationToken = default);
    Task<StaffAcademicProfileResponseDTO> GetStaffProfileAsync(long institutionId, long staffMemberId, CancellationToken cancellationToken = default);
    Task<StaffTeachingAssignmentResponseDTO> SaveTeachingAssignmentAsync(StaffTeachingAssignmentRequestDTO request, long institutionId, long staffMemberId, long actorStaffMemberId, CancellationToken cancellationToken = default);
    Task<ClassTeacherAssignmentResponseDTO> SaveClassTeacherAssignmentAsync(ClassTeacherAssignmentRequestDTO request, long institutionId, long staffMemberId, long actorStaffMemberId, CancellationToken cancellationToken = default);
    Task DeleteTeachingAssignmentAsync(long id, long institutionId, CancellationToken cancellationToken = default);
    Task DeleteClassTeacherAssignmentAsync(long id, long institutionId, CancellationToken cancellationToken = default);
}
