// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines persistence operations for gender data and option projections.
// ================================================================

using skolenza.application.DTOS.Common;
using skolenza.application.DTOS.Gender;
using skolenza.domain.Entity;

namespace skolenza.application.Interfaces.IRepositories
{
    /// <summary>
    /// Defines persistence operations for gender data and option projections.
    /// </summary>
    public interface IGenderRepository
    {
        /// <summary>
        /// Gets available gender options as query data.
        /// </summary>
        /// <param name="dto">The option query criteria.</param>
        /// <returns>The available gender option projections.</returns>
        Task<List<GetGenderOptionResponseDTO>> GetOptionAsync(GetOptionRequestDTO dto);

        Task<Gender> GetByIdAsync(long id);
        Task<IEnumerable<Gender>> GetAllAsync();
        Task AddAsync(Gender gender);
        Task UpdateAsync(Gender gender);
        Task DeleteAsync(long id);
    }
}
