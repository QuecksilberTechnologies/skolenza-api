using skolenza.application.DTOS.Staff;

namespace skolenza.application.Interfaces.IRepositories;

public interface IStaffProfessionalStructureRepository
{
    Task SeedInstitutionMastersAsync(long institutionId, long? actorStaffMemberId, CancellationToken cancellationToken = default);
    Task<List<StaffProfessionalMasterResponseDTO>> GetMastersAsync(StaffProfessionalMasterType type, long institutionId, bool activeOnly, CancellationToken cancellationToken = default);
    Task<StaffProfessionalMasterResponseDTO> SaveMasterAsync(StaffProfessionalMasterType type, StaffProfessionalMasterRequestDTO request, long institutionId, long actorStaffMemberId, CancellationToken cancellationToken = default);
    Task DeleteMasterAsync(StaffProfessionalMasterType type, int id, long institutionId, long actorStaffMemberId, CancellationToken cancellationToken = default);
    Task<List<StaffPositionMasterResponseDTO>> GetPositionsAsync(long institutionId, bool activeOnly, short? staffCategoryId, CancellationToken cancellationToken = default);
    Task<StaffPositionMasterResponseDTO> SavePositionAsync(StaffPositionMasterRequestDTO request, long institutionId, long actorStaffMemberId, CancellationToken cancellationToken = default);
    Task DeletePositionAsync(int id, long institutionId, long actorStaffMemberId, CancellationToken cancellationToken = default);
    Task AssignPrimaryPositionAsync(long institutionId, long staffMemberId, int positionId, DateOnly effectiveFrom, long actorStaffMemberId, CancellationToken cancellationToken = default);
    Task ReplacePrimaryPositionAsync(long institutionId, long staffMemberId, int positionId, DateOnly effectiveFrom, long actorStaffMemberId, CancellationToken cancellationToken = default);
    Task<StaffProfessionalProfileResponseDTO?> GetStaffProfileAsync(long institutionId, long staffMemberId, CancellationToken cancellationToken = default);
    Task VerifyCapabilityAsync(long institutionId, long assignmentId, bool approved, string? remark, long actorStaffMemberId, CancellationToken cancellationToken = default);
}
