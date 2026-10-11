// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the repository contract for  Ticket Classification Repository.
// ================================================================

using skolenza.application.DTOS.Pagination;
using skolenza.application.DTOS.TicketDTO.Classification;
using skolenza.domain.Entity; 
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; 
namespace skolenza.application.Interfaces.IRepositories
{
   
        /// <summary>
        /// Defines the contract for TicketClassificationRepository.
        /// </summary>
        public interface ITicketClassificationRepository
        {
        /// <summary>
        /// Add new ticket classification.
        /// </summary>
           Task<TicketClassification?> AddAsync(TicketClassification entity);

        /// <summary>
        /// Get classification by Id.
        /// </summary>
           Task<GetClassificationResponseDTO?> GetByIdAsync(long id, long institutionId);

            /// <summary>
            /// Get all active classifications.
            /// </summary>
            Task<PagedResponseDTO<GetClassificationResponseDTO>> GetAllAsync(long institutionId, GetAllClassificationRequestDTO dto);
            Task<List<GetClassificationResponseDTO>> GetDDLAsync(bool isActive, long institutionId);

            /// <summary>
            /// Soft delete classification (mark inactive).
            /// </summary>
            Task<bool> DeleteAsync(DeleteClassificationRequestDTO dTO, long staffMemberId);

            /// <summary>
            /// Update existing classification details.
            /// </summary>
            Task<TicketClassification?> GetByIdForInstitutionAsync(int id, long institutionId);
            Task<TicketClassification?> UpdateAsync(TicketClassification entity);
        }
   
}
