// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the repository contract for  Institution Module Configuration Repository.
// ================================================================

using skolenza.application.DTOs.Module;
using skolenza.application.DTOs.Institution;
using skolenza.application.Wrappers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.Interfaces.IRepositories
{
   /// <summary>
   /// Defines the contract for InstitutionModuleConfigurationRepository.
   /// </summary>
   public interface  IInstitutionModuleConfigurationRepository
    {
        Task  CreateByDefaultEnabledModulesAsync(long InstitutionId, List<InstitutionEnabledModule> moduleEntities, List<InstitutionEnabledOperation> operationEntities);

        /// <summary>
        /// Stages only missing entitlement rows from the Institution's active subscription plan.
        /// Existing Institution rows are never changed or duplicated.
        /// </summary>
        /// <param name="institutionId">The authoritative Institution identifier.</param>
        /// <param name="addedById">The authenticated Host user performing the explicit sync.</param>
        /// <param name="cancellationToken">The token used to observe cancellation.</param>
        /// <returns>The staged synchronization counts, or <see langword="null"/> when no active plan exists.</returns>
        Task<InstitutionPlanEntitlementSyncResult?> SynchronizeMissingActivePlanEntitlementsAsync(
            long institutionId,
            long addedById,
            CancellationToken cancellationToken = default);
        
        //yeh function sirf enabled module or operation laata hai , login mei bhi used

        //Task<List<InstitutionEnabledModule>> GetAllEnabledTrueModulesWithOperationsByInstitutionIdAsync(long? InstitutionId);
        Task <GetModuleHierarchyResponseDTO> GetAllInstitutionEnabledModulesAsync(InstitutionEnabledModuleRequestDTO dto);
        Task<GetModuleHierarchyResponseDTO> GetAllInstitutionEnabledModulesAsync(InstitutionEnabledOperation dto);
         
       

       
    }
}
