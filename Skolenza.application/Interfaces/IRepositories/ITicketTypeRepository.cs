// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the repository contract for  Ticket Type Repository.
// ================================================================

using skolenza.application.DTOs.PageTypeEnum;
using skolenza.application.DTOS.Pagination;
using skolenza.application.DTOS.TicketDTO.Classification;
using skolenza.application.DTOS.TicketDTO.TicketType;
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
    /// Defines the contract for TicketTypeRepository.
    /// </summary>
    public interface ITicketTypeRepository
    {


        Task<List<GetDDLTicketTypeResponseDTO>> GetDDLAsync(bool isActive, long institutionId);
        public Task<TicketType?> AddAsync(TicketType entity);
        public Task<PagedResponseDTO<GetTicketTypeResponseDTO>> AllAsync(long institutionId, GetTicketTypeRequestDTO dTO);
        public Task<List<GetTicketTypeResponseDTO>> AllByHeaderIdAsync(long institutionId, GetTicketTypeByHeaderIdRequestDTO dTO);
        public Task<List<GetTicketTypeRoleResponseDTO>> AllByRoleIdAsync(GetTicketTypeByRoleIdRequestDTO dTO);
        public Task<GetTicketTypeResponseDTO?> GetByIdAsync(long id, bool isActive);
        public  Task<bool> DeleteAsync(long id, long staffMemberId);
        public Task<TicketType?> GetByIdForInstitutionAsync(long id, long institutionId);
        public Task<TicketType?> UpdateAsync(TicketType entity);




    }
}
