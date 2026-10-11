// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines persistence operations for location data and option projections.
// ================================================================

using skolenza.application.DTOS.Location;
using skolenza.domain.Entity;

namespace skolenza.application.Interfaces.IRepositories
{
    /// <summary>
    /// Defines persistence operations for location data and option projections.
    /// </summary>
    public interface ILocationRepository
    {
        /// <summary>
        /// Gets active country option projections.
        /// </summary>
        Task<List<GetCountryOptionResponseDTO>> GetCountryOptionAsync(GetCountryOptionRequestDTO dto);

        /// <summary>
        /// Gets active state option projections for a country.
        /// </summary>
        Task<List<GetStateOptionResponseDTO>> GetStateOptionAsync(GetStateOptionRequestDTO dto);

        /// <summary>
        /// Gets active district option projections for a state.
        /// </summary>
        Task<List<GetDistrictOptionResponseDTO>> GetDistrictOptionAsync(GetDistrictOptionRequestDTO dto);

        Task<List<GetLocalityOptionResponseDTO>> GetLocalityOptionAsync(GetLocalityOptionRequestDTO dto);

        Task<List<GetLocalityTypeOptionResponseDTO>> GetLocalityTypeOptionAsync(GetLocalityTypeOptionRequestDTO dto);

        /// <summary>
        /// Determines whether an active state exists for the supplied identifier.
        /// </summary>
        Task<bool> IsActiveStateAsync(int stateId);

        Task<bool> IsActiveDistrictAsync(int districtId);

        Task<List<Country>> GetAllAsync();
        Task<Country> GetByIdAsync(int id);
        Task AddAsync(Country country);
        Task UpdateAsync(Country country);
        Task DeleteAsync(int id);
    }
}
