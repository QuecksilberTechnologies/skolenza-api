// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the repository contract for  Reporting Type Repository.
// ================================================================

using skolenza.application.DTOs.Manager.ReportingType;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; 
using skolenza.domain.Entity; 
using MediatR;
using skolenza.application.DTOS.Pagination;

namespace skolenza.application.Interfaces.IRepositories
{
    /// <summary>
    /// Defines the contract for ReportingTypeRepository.
    /// </summary>
    public interface IReportingTypeRepository
    {
        public Task<ReportingType?> AddAsync(ReportingType entity);
        public Task<PagedResponseDTO<GetReportingTypeResponseDTO>> AllAsync(long institutionId, GetReportingTypeRequestDTO dTO);
        public Task<GetReportingTypeResponseDTO?> GetByIdAsync(long id);
        public Task<bool> DeleteAsync(long id, long staffMemberId);
        public Task<bool> UpdateAsync(UpdateReportingTypeRequestDTO dto);
    }
}
