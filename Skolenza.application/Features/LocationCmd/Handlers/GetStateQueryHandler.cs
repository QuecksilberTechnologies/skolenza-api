// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Handles requests for active state option projections.
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
    /// Represents a request for active state options.
    /// </summary>
    public class GetStateQuery : IRequest<ApiResponse<List<GetStateOptionResponseDTO>>>
    {
        public GetStateOptionRequestDTO? DTO { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GetStateQuery"/> class.
        /// </summary>
        public GetStateQuery(GetStateOptionRequestDTO dto)
        {
            DTO = dto;
        }
    }

    #endregion

    #region Handler

    /// <summary>
    /// Handles requests for active state option projections.
    /// </summary>
    public class GetStateQueryHandler : IRequestHandler<GetStateQuery, ApiResponse<List<GetStateOptionResponseDTO>>>
    {
        #region Fields

        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetStateQueryHandler> _logger;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="GetStateQueryHandler"/> class.
        /// </summary>
        public GetStateQueryHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetStateQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        #endregion

        #region Handle

        /// <summary>
        /// Retrieves state option data and constructs the successful API response.
        /// </summary>
        public async Task<ApiResponse<List<GetStateOptionResponseDTO>>> Handle(
            GetStateQuery request,
            CancellationToken cancellationToken)
        {
            if (request?.DTO == null || !request.DTO.TodaysDate.HasValue)
            {
                throw new ValidationErrorException(AppConstants.ErrorMessages.RequiredDataMissing);
            }

            if (request.DTO.CountryId <= 0)
            {
                throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
            }

            var states = await _unitOfWork.LocationRepository.GetStateOptionAsync(request.DTO);
            _logger.LogInformation(
                "Retrieved {Count} state options for country {CountryId}.",
                states.Count,
                request.DTO.CountryId);

            // Build the application response in the handler layer.
            return ApiResponse<List<GetStateOptionResponseDTO>>.Success(
                states,
                AppConstants.SuccessMessages.StatesRetrieved);
        }

        #endregion
    }

    #endregion
}
