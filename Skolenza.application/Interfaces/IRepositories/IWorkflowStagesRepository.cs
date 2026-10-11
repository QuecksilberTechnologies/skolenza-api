
using skolenza.application.DTOs.WorkflowStage;
 
 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.Interfaces.IRepositories
{
    public interface IWorkflowStagesRepository
    {

        public Task<List<GetWorkflowStageResponseDTO>> AddAsync(CreateWorkflowStageRequestDTO dTO);
        public Task<List<GetWorkflowStageResponseDTO>> AllAsync(GetWorkflowStageRequestDTO dTO);      
        public Task<GetWorkflowStageResponseDTO?> GetByIdAsync(long id);
        public Task<bool> DeleteAsync(long id, long staffMemberId);
        public Task<bool> UpdateAsync(UpdateWorkflowStageRequestDTO dto);
    }
}
