using skolenza.application.DTOs.Registration;
using skolenza.application.DTOs.SubscriptionModule;
using skolenza.application.DTOs.Institution;

 
using System;
using System.Collections.Generic;
    using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;
    using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

    namespace skolenza.application.Interfaces.IRepositories
    {
        public interface IInstitutionSubscriptionRepository
        {
        /// <summary>
        /// Get subscription detail for a institution based on filter.
        /// </summary>
        Task<List<InstitutionSubscriptionPlanResponseDTO>> GetInstitutionSubscriptionPlanInfoAsync(InstitutionSubscriptionPlanRequestDTO dto);

         /// <summary>
         /// Get all institution subscriptions (optional filter).
         /// </summary>
         Task<List<InstitutionSubscription>> GetAllInstitutionSubscriptionsAsync(InstitutionSubscription? filter = null);

         Task<InstitutionSubscriptionPlanResponseDTO> GetValidateInstitutionPlan(InstitutionSubscriptionPlanRequestDTO dto);


        /// <summary>
        /// Add a new institution subscription.
        /// </summary>
        Task<InstitutionSubscription> AddInstitutionSubscriptionAsync(InstitutionSubscription subscription);

            /// <summary>
            /// Update an existing institution subscription.
            /// </summary>
            Task UpdateInstitutionSubscriptionAsync(InstitutionSubscription subscription);

            /// <summary>
            /// Soft delete a institution subscription.
            /// </summary>
            Task<bool> DeleteInstitutionSubscriptionAsync(long id);
        }
    }

 
