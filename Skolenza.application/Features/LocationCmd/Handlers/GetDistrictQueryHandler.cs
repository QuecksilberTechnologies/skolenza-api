// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Handles requests for active district option projections.
// ================================================================

using skolenza.application.Constants;
using skolenza.application.DTOS.Location;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.LocationCmd.Handlers
{
    #region Query

    /// <summary>
    /// Represents a request for active district options.
    /// </summary>
    public class GetDistrictQuery : IRequest<ApiResponse<List<GetDistrictOptionResponseDTO>>>
    {
        public GetDistrictOptionRequestDTO? DTO { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GetDistrictQuery"/> class.
        /// </summary>
        public GetDistrictQuery(GetDistrictOptionRequestDTO dto)
        {
            DTO = dto;
        }
    }

    #endregion

    #region Handler

    /// <summary>
    /// Handles requests for active district option projections.
    /// </summary>
    public class GetDistrictQueryHandler : IRequestHandler<GetDistrictQuery, ApiResponse<List<GetDistrictOptionResponseDTO>>>
    {
        #region Fields

        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetDistrictQueryHandler> _logger;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="GetDistrictQueryHandler"/> class.
        /// </summary>
        public GetDistrictQueryHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetDistrictQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        #endregion

        #region Handle

        /// <summary>
        /// Retrieves district option data and constructs the successful API response.
        /// </summary>
        public async Task<ApiResponse<List<GetDistrictOptionResponseDTO>>> Handle(
            GetDistrictQuery request,
            CancellationToken cancellationToken)
        {
            if (request?.DTO == null || !request.DTO.TodaysDate.HasValue)
            {
                throw new ValidationErrorException(AppConstants.ErrorMessages.RequiredDataMissing);
            }

            if (request.DTO.StateId <= 0)
            {
                throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
            }

            var stateExists = await _unitOfWork.LocationRepository.IsActiveStateAsync(request.DTO.StateId);
            if (!stateExists)
            {
                throw new NotFoundException(AppConstants.ErrorMessages.ResourceNotFound);
            }

            var districts = await _unitOfWork.LocationRepository.GetDistrictOptionAsync(request.DTO);
            _logger.LogInformation(
                "Retrieved {Count} district options for state {StateId}.",
                districts.Count,
                request.DTO.StateId);

            // Build the application response in the handler layer.
            return ApiResponse<List<GetDistrictOptionResponseDTO>>.Success(
                districts,
                AppConstants.SuccessMessages.DistrictsRetrieved);
        }

        #endregion
    }

    #endregion
}
