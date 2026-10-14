using skolenza.application.DTOS.Staff;

namespace skolenza.application.Interfaces.IRepositories;

public interface IStaffCategoryRepository
{
    Task<IReadOnlyList<StaffCategoryOptionDTO>> GetActiveOptionsAsync(CancellationToken cancellationToken);
    Task<bool> IsActiveAsync(short staffCategoryId, CancellationToken cancellationToken);
    Task<IReadOnlyList<HostStaffCategoryResponseDTO>> GetAllAsync(CancellationToken cancellationToken);
    Task<HostStaffCategoryResponseDTO> SaveAsync(SaveHostStaffCategoryRequestDTO request, CancellationToken cancellationToken);
}
