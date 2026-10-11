// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles the read-only request to retrieve Get All Gender.
// ================================================================

using skolenza.application.DTOs.Gender;
using skolenza.application.DTOs.Leave;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using skolenza.application.Features.GenderCmd.Queries;
using skolenza.application.Features.LeaveCmd.Handlers;
using skolenza.application.Features.LeaveCmd.Queries;
using skolenza.application.Interfaces;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.GenderCmd.Queries
{
    #region Query

    /// <summary>
    /// Represents the read-only request to retrieve Get All Gender.
    /// </summary>
public class GetAllGenderQuery : IRequest<ApiResponse<List<GetGenderResponseDTO>>>
    {
        public GetGenderRequestDTO DTO { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="GetAllGenderQuery"/> class.
        /// </summary>

        public GetAllGenderQuery(GetGenderRequestDTO dTO)
        {
            DTO = dTO;
        }
    }

    #endregion
}

namespace skolenza.application.Features.GenderCmd.Handlers
{
    /// <summary>
    /// Handles the request to Get All Gender.
    /// </summary>
public class GetAllGenderQueryHandler : IRequestHandler<GetAllGenderQuery, ApiResponse<List<GetGenderResponseDTO>>>
    {
        #region Fields

        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetAllGenderQueryHandler> _logger;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="GetAllGenderQueryHandler"/> class.
        /// </summary>


        public GetAllGenderQueryHandler(IMapper mapper, IUnitOfWork unitOfWork, ILogger<GetAllGenderQueryHandler> logger)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }
        #endregion

        #region Handler
        /// <summary>
        /// Processes the supplied GetAllGenderQuery.
        /// </summary>
        /// <param name="request">The request to process.</param>
        /// <param name="cancellationToken">The token used to observe cancellation.</param>
        /// <returns>The response produced for the request.</returns>


        public async Task<ApiResponse<List<GetGenderResponseDTO>>> Handle(GetAllGenderQuery request, CancellationToken cancellationToken)
        {
            try
            {
               
                if (request.DTO == null)
                {
                    return new ApiResponse<List<GetGenderResponseDTO>>
                    {
                        IsSucceeded = false,
                        Message = "No Gender list found.",
                        Data = new List<GetGenderResponseDTO>()
                    };
                }
                // 🔹 Repository se data fetch karo
                IEnumerable<Gender> genders = await _unitOfWork.GenderRepository.GetAllAsync();

                // 🔹 Validation: Agar list null ya empty hai
                if (genders == null || !genders.Any())
                {
                    _logger.LogWarning("⚠️ No Genders found in database.");

                    return new ApiResponse<List<GetGenderResponseDTO>>
                    {
                        IsSucceeded = false,
                        Message = "No Genders found.",
                        Data = new List<GetGenderResponseDTO>()
                    };
                }

                // 🔹 Map entity -> DTO
                var GenderDTOs = _mapper.Map<List<GetGenderResponseDTO>>(genders);

                _logger.LogInformation("✅ Successfully retrieved {Count} Genders.", GenderDTOs.Count);

                return new ApiResponse<List<GetGenderResponseDTO>>
                {
                    IsSucceeded = true,
                    Message = "Genders fetched successfully.",
                    Data = GenderDTOs
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error occurred while fetching LeaveTypes.");

                return new ApiResponse<List<GetGenderResponseDTO>>
                {
                    IsSucceeded = false,
                    Message = "An error occurred while processing your request.",
                    Data = new List<GetGenderResponseDTO>()
                };
            }
        }


    
        #endregion
}
}
