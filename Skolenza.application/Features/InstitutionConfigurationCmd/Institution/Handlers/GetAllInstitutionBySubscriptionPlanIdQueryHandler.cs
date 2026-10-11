// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles the read-only request to retrieve Get All Institution By Subscription Plan Id.
// ================================================================

using skolenza.application.DTOs.Operation;
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
using skolenza.application.DTOs.Registration;
using skolenza.application.Features.InstitutionConfigurationCmd.Institution.Queries;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.IRepositories;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.InstitutionConfigurationCmd.Institution.Queries
{
    #region Query

    /// <summary>
    /// Represents the read-only request to retrieve Get All Institution By Subscription Plan Id.
    /// </summary>
public class GetAllInstitutionBySubscriptionPlanIdQuery : IRequest<ApiResponse<List<InstitutionResponseDTO>>>
    {
        public InstitutionRequestDTO? institutionRequestDTO { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="GetAllInstitutionBySubscriptionPlanIdQuery"/> class.
        /// </summary>

        public GetAllInstitutionBySubscriptionPlanIdQuery(InstitutionRequestDTO institutionRequestDTO)
        {
            this.institutionRequestDTO = institutionRequestDTO;
        }
    }

    #endregion
}

namespace skolenza.application.Features.InstitutionConfigurationCmd.Institution.Handlers
{
    /// <summary>
    /// Handles the request to Get All Institution By Subscription Plan Id.
    /// </summary>
public class GetAllInstitutionBySubscriptionPlanIdQueryHandler : IRequestHandler<GetAllInstitutionBySubscriptionPlanIdQuery, ApiResponse<List<InstitutionResponseDTO>>>
    {
        #region Fields

        private readonly IInstitutionRepository _institutionRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetAllInstitutionBySubscriptionPlanIdQueryHandler> _logger;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="GetAllInstitutionBySubscriptionPlanIdQueryHandler"/> class.
        /// </summary>


        public GetAllInstitutionBySubscriptionPlanIdQueryHandler(
        IInstitutionRepository institutionRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<GetAllInstitutionBySubscriptionPlanIdQueryHandler> logger)
        {
            _institutionRepository = institutionRepository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }
        #endregion

        #region Handler
        /// <summary>
        /// Processes the supplied GetAllInstitutionBySubscriptionPlanIdQuery.
        /// </summary>
        /// <param name="request">The request to process.</param>
        /// <param name="cancellationToken">The token used to observe cancellation.</param>
        /// <returns>The response produced for the request.</returns>


        public async Task<ApiResponse<List<InstitutionResponseDTO>>> Handle(GetAllInstitutionBySubscriptionPlanIdQuery request, CancellationToken cancellationToken)
        {
            try
            {

                // ✅ Mapping the DTO to entity
                var institutionDTO = _mapper.Map<skolenza.domain.Entity.Institution>(request.institutionRequestDTO);

                // ✅ Fetching from DB
                List<skolenza.domain.Entity.Institution> institutions = await _unitOfWork.InstitutionRepository.GetAllInstitutionBySubscriptionIdAsync(institutionDTO);

                // ✅ Mapping to response DTO
                var getAllInstitutionsDTOs = _mapper.Map<List<InstitutionResponseDTO>>(institutions);

                // ✅ Condition: if null or empty
                if (getAllInstitutionsDTOs == null || !getAllInstitutionsDTOs.Any())
                {
                    _logger.LogWarning("No institutions found.");
                    return new ApiResponse<List<InstitutionResponseDTO>>
                    {
                        IsSucceeded = false,
                        Message = "No institutions found.",
                        Data = new List<InstitutionResponseDTO>() // can also return null if needed
                    };
                }

                _logger.LogInformation("Successfully retrieved {Count} Institutions.", getAllInstitutionsDTOs.Count);

                return new ApiResponse<List<InstitutionResponseDTO>>
                {
                    IsSucceeded = true,
                    Message = "Institutions fetched successfully.",
                    Data = getAllInstitutionsDTOs
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching Institutions.");
                return new ApiResponse<List<InstitutionResponseDTO>>
                {
                    IsSucceeded = false,
                    Message = "Institutions not fetched.",
                    Data = null
                };
            }
        }


    
        #endregion
}
}
