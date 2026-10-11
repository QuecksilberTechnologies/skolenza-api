// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles the read-only request to retrieve Get All Travel Mode Type.
// ================================================================

using skolenza.application.DTOs.Transport;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using skolenza.application.Features.LeaveCmd.Handlers;
using skolenza.application.Features.LeaveCmd.Queries;
using skolenza.application.Features.TransportCmd.Queries;
using skolenza.application.Interfaces;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.TransportCmd.Queries
{
    #region Query

    /// <summary>
    /// Represents the read-only request to retrieve Get All Travel Mode Type.
    /// </summary>
public class GetAllTravelModeTypeQuery : IRequest<ApiResponse<List<GetAllTravelModeDTO>>>
    {
        public TravelModeRequestDTO? travelModeRequestDTO { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="GetAllTravelModeTypeQuery"/> class.
        /// </summary>

        public GetAllTravelModeTypeQuery(TravelModeRequestDTO clientTypeRequestDTO)
        {
            this.travelModeRequestDTO = travelModeRequestDTO;
        }
    }

    #endregion
}

namespace skolenza.application.Features.TransportCmd.Handlers
{
    /// <summary>
    /// Handles the request to Get All Travel Mode Type.
    /// </summary>
public class GetAllTravelModeTypeQueryHandler : IRequestHandler<GetAllTravelModeTypeQuery, ApiResponse<List<GetAllTravelModeDTO>>>
    {
        #region Fields

        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetAllTravelModeTypeQueryHandler> _logger;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="GetAllTravelModeTypeQueryHandler"/> class.
        /// </summary>


        public GetAllTravelModeTypeQueryHandler(IMapper mapper, IUnitOfWork unitOfWork, ILogger<GetAllTravelModeTypeQueryHandler> logger)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }
        #endregion

        #region Handler
        /// <summary>
        /// Processes the supplied GetAllTravelModeTypeQuery.
        /// </summary>
        /// <param name="request">The request to process.</param>
        /// <param name="cancellationToken">The token used to observe cancellation.</param>
        /// <returns>The response produced for the request.</returns>

        public async Task<ApiResponse<List<GetAllTravelModeDTO>>> Handle(GetAllTravelModeTypeQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // ✅ Correcting the method call
                List<TravelMode> travelModes = await _unitOfWork.TravelRepository.GetAllTravelModeTypeAsync();

                //if (roles == null || !roles.Any())
                //{
                //    _logger.LogWarning("No roles found.");
                //    return new ApiResponse<List<GetAllRoleDTO>>(false, "No roles found", new List<GetAllRoleDTO>());
                //}

                //// ✅ Map Role entities to DTOs
                var travelDTOs = _mapper.Map<List<GetAllTravelModeDTO>>(travelModes);

                _logger.LogInformation("Successfully retrieved {Count} travelModes.", travelDTOs.Count);
                return new ApiResponse<List<GetAllTravelModeDTO>>
                {
                    IsSucceeded = true,
                    Message = "travelModes fetched successfully.",
                    Data = travelDTOs
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching roles.");
                return new ApiResponse<List<GetAllTravelModeDTO>>
                {
                    IsSucceeded = false,
                    Message = "Categories fetched successfully.",
                    Data = null
                };
            }
        }
         

       
    
        #endregion
}
}
