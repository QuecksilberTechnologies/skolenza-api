// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Handles requests for active country option projections.
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
    /// Represents a request for active country options.
    /// </summary>
    public class GetCountryQuery : IRequest<ApiResponse<List<GetCountryOptionResponseDTO>>>
    {
        public GetCountryOptionRequestDTO DTO { get; set; } = default!;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetCountryQuery"/> class.
        /// </summary>
        public GetCountryQuery(GetCountryOptionRequestDTO dto)
        {
            DTO = dto;
        }
    }

    #endregion

    #region Handler

    /// <summary>
    /// Handles requests for active country option projections.
    /// </summary>
    public class GetCountryQueryHandler : IRequestHandler<GetCountryQuery, ApiResponse<List<GetCountryOptionResponseDTO>>>
    {
        #region Fields

        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetCountryQueryHandler> _logger;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="GetCountryQueryHandler"/> class.
        /// </summary>
        public GetCountryQueryHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetCountryQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        #endregion

        #region Handle

        /// <summary>
        /// Retrieves country option data and constructs the successful API response.
        /// </summary>
        public async Task<ApiResponse<List<GetCountryOptionResponseDTO>>> Handle(
            GetCountryQuery request,
            CancellationToken cancellationToken)
        {
            if (request?.DTO == null || !request.DTO.TodaysDate.HasValue)
            {
                throw new ValidationErrorException(AppConstants.ErrorMessages.RequiredDataMissing);
            }

            var countries = await _unitOfWork.LocationRepository.GetCountryOptionAsync(request.DTO);
            _logger.LogInformation("Retrieved {Count} country options.", countries.Count);

            // Build the application response in the handler layer.
            return ApiResponse<List<GetCountryOptionResponseDTO>>.Success(
                countries,
                AppConstants.SuccessMessages.CountriesRetrieved);
        }

        #endregion
    }

    #endregion
}
