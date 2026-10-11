using GetStaffEmploymentTypeResponseDTO = skolenza.application.DTOs.StaffType.GetStaffEmploymentTypeResponseDTO;
using skolenza.application.DTOS.Staff.Type;
using skolenza.domain.Entity;

namespace skolenza.application.Interfaces.IRepositories;

public interface IStaffEmploymentTypeRepository
{
    Task<StaffEmploymentType?> GetStaffEmploymentTypeByIdAsync(long institutionId, int staffEmploymentTypeId, CancellationToken cancellationToken);
    Task<StaffEmploymentType?> GetForUpdateAsync(long institutionId, int staffEmploymentTypeId, CancellationToken cancellationToken);
    Task<List<GetStaffEmploymentTypeResponseDTO>> GetAllAsync(long institutionId, CancellationToken cancellationToken);
    Task<bool> NameExistsAsync(long institutionId, string typeName, int? excludeStaffEmploymentTypeId, CancellationToken cancellationToken);
    Task<bool> HasDeletionDependenciesAsync(long institutionId, int staffEmploymentTypeId, CancellationToken cancellationToken);
    Task<GetStaffEmploymentTypeResponseDTO> CreateAsync(long institutionId, long actorId, CreateStaffEmploymentTypeDTO request,
        CancellationToken cancellationToken);
    Task<GetStaffEmploymentTypeResponseDTO> UpdateAsync(StaffEmploymentType entity, CancellationToken cancellationToken);
    Task<bool> SoftDeleteAsync(StaffEmploymentType entity, CancellationToken cancellationToken);
    Task<int> EnsureOnboardingTypeAsync(long institutionId, long actorId, CancellationToken cancellationToken);
}
