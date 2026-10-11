using skolenza.application.DTOS.TicketDTO.Header;
using skolenza.application.DTOS.TicketDTO.TicketType;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using skolenza.domain.Entity;
using MediatR;
using skolenza.application.DTOS.TicketDTO.Creation;

namespace skolenza.application.Interfaces.IRepositories
{

    public interface IAssetTicketDetailRepository
  {
    /// <summary>
    /// Add new AssetTicketType .
    /// </summary>
    public Task<List<GetAssetTicketResponseDTO>> AddAsync(AddTicketTypeRequestDTO dTO);

      
        /// <summary>
        /// Get all AssetTicketType By filter header.
        /// </summary>
        Task<List<GetAssetTicketResponseDTO>> GetAllByFilerAsync(GetAssetTicketFilterRequestDTO dTO);

    /// <summary>
    /// Soft delete AssetTicketType (mark inactive).
    /// </summary>
    Task<bool> DeleteAsync(DeleteAssetTicketRequestDTO dTO);
         
        /// <summary>
        /// Update existing AssetTicketType details.
        /// </summary>
        Task<GetAssetTicketResponseDTO?> UpdateAsync(UpdateHeaderRequestDTO dTO);
        
    }

}
