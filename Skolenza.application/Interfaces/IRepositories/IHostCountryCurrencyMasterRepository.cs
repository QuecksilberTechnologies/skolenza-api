using skolenza.application.DTOS.Pagination;
using skolenza.application.DTOS.Student;

namespace skolenza.application.Interfaces.IRepositories;

public interface IHostCountryCurrencyMasterRepository
{
    Task<PagedResponseDTO<CountryCurrencyMasterResponseDTO>> GetPageAsync(
        CountryCurrencyMasterFilterDTO filter,
        CancellationToken cancellationToken);

    Task<CountryCurrencyMasterResponseDTO> SaveAsync(
        long? id,
        CountryCurrencyMasterRequestDTO request,
        long hostUserId,
        CancellationToken cancellationToken);

    Task DeactivateAsync(long id, long hostUserId, CancellationToken cancellationToken);
}
