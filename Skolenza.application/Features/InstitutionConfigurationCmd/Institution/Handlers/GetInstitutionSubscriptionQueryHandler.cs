// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles the read-only request to retrieve Get Institution Subscription.
// ================================================================

using skolenza.application.DTOs.Registration;
using skolenza.application.DTOs.Institution;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using skolenza.application.Features.InstitutionConfigurationCmd.Institution.Queries;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.IRepositories;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace skolenza.application.Features.InstitutionConfigurationCmd.Institution.Queries
{
    #region Query

    /// <summary>
    /// Represents the read-only request to retrieve Get Institution Subscription.
    /// </summary>
public class GetInstitutionSubscriptionQuery : IRequest<ApiResponse<List<InstitutionSubscriptionPlanResponseDTO>>>
    {
        public InstitutionSubscriptionPlanRequestDTO? DTO { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="GetInstitutionSubscriptionQuery"/> class.
        /// </summary>

        public GetInstitutionSubscriptionQuery(InstitutionSubscriptionPlanRequestDTO dTO)
        {
            this.DTO = dTO;
        }
    }

    #endregion
}

namespace skolenza.application.Features.InstitutionConfigurationCmd.Institution.Handlers
{
    /// <summary>
    /// Handles the request to Get Institution Subscription.
    /// </summary>
public class GetInstitutionSubscriptionQueryHandler
        : IRequestHandler<GetInstitutionSubscriptionQuery, ApiResponse<List<InstitutionSubscriptionPlanResponseDTO>>>
    {
        #region Fields

        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetInstitutionSubscriptionQueryHandler> _logger;
        private readonly IInstitutionSubscriptionRepository _institutionSubscriptionRepository;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="GetInstitutionSubscriptionQueryHandler"/> class.
        /// </summary>


        public GetInstitutionSubscriptionQueryHandler(
            IInstitutionSubscriptionRepository institutionSubscriptionRepository,
            IMapper mapper,
            IUnitOfWork unitOfWork,
            ILogger<GetInstitutionSubscriptionQueryHandler> logger)
        {
            _institutionSubscriptionRepository = institutionSubscriptionRepository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }
        #endregion

        #region Handler
        /// <summary>
        /// Processes the supplied GetInstitutionSubscriptionQuery.
        /// </summary>
        /// <param name="request">The request to process.</param>
        /// <param name="cancellationToken">The token used to observe cancellation.</param>
        /// <returns>The response produced for the request.</returns>


        public async Task<ApiResponse<List<InstitutionSubscriptionPlanResponseDTO>>> Handle(GetInstitutionSubscriptionQuery request, CancellationToken cancellationToken)
        {
            try
            {
                if (request.DTO == null)
                {
                    _logger.LogWarning("GetInstitutionSubscriptionQuery DTO is null.");
                    return new ApiResponse<List<InstitutionSubscriptionPlanResponseDTO>>()
                    {
                        IsSucceeded = false,
                        Message = "Invalid request data.",
                        Data = null
                    };
                }

                _logger.LogInformation("Fetching institution subscription plan info for InstitutionId: {InstitutionId}", request.DTO.InstitutionId);

                // ✅ Repository call (returns list)
                List <InstitutionSubscriptionPlanResponseDTO> responseDTOs = await _institutionSubscriptionRepository.GetInstitutionSubscriptionPlanInfoAsync(request.DTO);

                if (responseDTOs == null || responseDTOs.Count == 0)
                {
                    _logger.LogWarning("No institution subscription plan found for InstitutionId: {InstitutionId}", request.DTO.InstitutionId);
                    return new ApiResponse<List<InstitutionSubscriptionPlanResponseDTO>>()
                    {
                        IsSucceeded = false,
                        Message = "No institution subscription plan found.",
                        Data = null
                    };
                }

             
                _logger.LogInformation("Successfully retrieved {Count} institution subscription records for InstitutionId: {InstitutionId}",
                    responseDTOs.Count, request.DTO.InstitutionId);

                return new ApiResponse<List<InstitutionSubscriptionPlanResponseDTO>>()
                {
                    IsSucceeded = true,
                    Message = "Institution subscription plan(s) fetched successfully.",
                    Data = responseDTOs
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching institution subscription plan.");
                return new ApiResponse<List<InstitutionSubscriptionPlanResponseDTO>>()
                {
                    IsSucceeded = false,
                    Message = "An error occurred while fetching institution subscription plan.",
                    Data = null
                };
            }
        }
    
        #endregion
}
}
