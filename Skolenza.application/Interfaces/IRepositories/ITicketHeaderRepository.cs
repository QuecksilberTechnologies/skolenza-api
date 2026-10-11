// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the repository contract for  Ticket Header Repository.
// ================================================================



using skolenza.application.DTOS.Pagination;
using skolenza.application.DTOS.TicketDTO.Header;
using skolenza.domain.Entity;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Model;

namespace skolenza.application.Interfaces.IRepositories
{

    /// <summary>
    /// Defines the contract for TicketHeaderRepository.
    /// </summary>
    public interface ITicketHeaderRepository
    {
        /// <summary>
        /// Add new ticket header.
        /// </summary>
        Task<TicketHeader?> AddAsync(TicketHeader entity);

        /// <summary>
        /// Get header by Id.
        /// </summary>
        Task<List<GetHeaderResponseDTO>> GetAllHeaderAsync(GetHeaderRequestDTO dto);
        Task<GetHeaderResponseDTO> GetByIdAsync(long headerId);
        Task<List<GetHeaderResponseDTO>> GetByClassificationIdAsync(long institutionId, GetTicketHeaderByClassifyIdRequestDTO dto);
        /// <summary>
        /// Get all active header.
        /// </summary>
        Task<List<GetHeaderResponseDTO>> GetAllAsync(GetHeaderRequestDTO dTO);

        /// <summary>
        /// Soft delete header (mark inactive).
        /// </summary>
        Task<bool> DeleteAsync(DeleteHeaderRequestDTO dTO, long StaffMemberId);
         
        /// <summary>
        /// Update existing header details.
        /// </summary>
        Task<TicketHeader?> GetByIdForInstitutionAsync(long id, long institutionId);
        Task<TicketHeader?> UpdateAsync(TicketHeader entity);
        
    }

}
