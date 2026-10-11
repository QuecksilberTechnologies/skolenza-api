// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles the read-only request to retrieve Get Institution Info By Id.
// ================================================================

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
    /// Represents the read-only request to retrieve Get Institution Info By Id.
    /// </summary>
public class GetInstitutionInfoByIdQuery : IRequest<ApiResponse<InstitutionResponseDTO>>
    {
        public long InstitutionId  { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="GetInstitutionInfoByIdQuery"/> class.
        /// </summary>

        public GetInstitutionInfoByIdQuery(long InstitutionId)
        {
            this.InstitutionId = InstitutionId;
        }
    }

    #endregion
}

namespace skolenza.application.Features.InstitutionConfigurationCmd.Institution.Handlers
{
    /// <summary>
    /// Handles the request to Get Institution Info By Id.
    /// </summary>
public class GetInstitutionInfoByIdQueryHandler : IRequestHandler<GetInstitutionInfoByIdQuery, ApiResponse<InstitutionResponseDTO>>
    {
        #region Fields

        private readonly IInstitutionRepository _institutionRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetInstitutionInfoByIdQueryHandler> _logger;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="GetInstitutionInfoByIdQueryHandler"/> class.
        /// </summary>


        public GetInstitutionInfoByIdQueryHandler(
            IInstitutionRepository institutionRepository,
            IMapper mapper,
            IUnitOfWork unitOfWork,
            ILogger<GetInstitutionInfoByIdQueryHandler> logger)
        {
            _institutionRepository = institutionRepository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }
        #endregion

        #region Handler
        /// <summary>
        /// Processes the supplied GetInstitutionInfoByIdQuery.
        /// </summary>
        /// <param name="request">The request to process.</param>
        /// <param name="cancellationToken">The token used to observe cancellation.</param>
        /// <returns>The response produced for the request.</returns>


        public async Task<ApiResponse<InstitutionResponseDTO>> Handle(GetInstitutionInfoByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Fetching institution information for InstitutionId: {InstitutionId}", request.InstitutionId);

                // ✅ Step 1: Repository call
                var institutionEntity = await _institutionRepository.GetByIdAsync(request.InstitutionId, true);

                // ✅ Step 2: Check null
                if (institutionEntity == null)
                {
                    _logger.LogWarning("No institution found with InstitutionId: {InstitutionId}", request.InstitutionId);
                    return new ApiResponse<InstitutionResponseDTO>
                    {
                        IsSucceeded = false,
                        Message = $"No institution found with InstitutionId: {request.InstitutionId}",
                        Data = null
                    };
                }

                // ✅ Step 3: Map entity to DTO
                var institutionDTO = _mapper.Map<InstitutionResponseDTO>(institutionEntity);

                // ✅ Step 4: Logging and Return
                _logger.LogInformation("Successfully retrieved institution information for InstitutionId: {InstitutionId}", request.InstitutionId);

                return new ApiResponse<InstitutionResponseDTO>
                {
                    IsSucceeded = true,
                    Message = "Institution information fetched successfully.",
                    Data = institutionDTO
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching institution information for InstitutionId: {InstitutionId}", request.InstitutionId);
                return new ApiResponse<InstitutionResponseDTO>
                {
                    IsSucceeded = false,
                    Message = "An error occurred while fetching institution information.",
                    Data = null
                };
            }
        }
    
        #endregion
}
}
