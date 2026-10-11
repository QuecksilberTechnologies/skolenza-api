// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Handles requests for available gender option projections.
// ================================================================

using skolenza.application.Constants;
using skolenza.application.DTOS.Common;
using skolenza.application.DTOS.Gender;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.GenderCmd.Handlers
{
    #region Query

    /// <summary>
    /// Represents a request for available gender options.
    /// </summary>
    public class GetGenderOptionQuery : IRequest<ApiResponse<List<GetGenderOptionResponseDTO>>>
    {
        public GetOptionRequestDTO OptionDTO { get; set; } = default!;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetGenderOptionQuery"/> class.
        /// </summary>
        /// <param name="optionDTO">The option query criteria.</param>
        public GetGenderOptionQuery(GetOptionRequestDTO optionDTO)
        {
            OptionDTO = optionDTO;
        }
    }

    #endregion

    #region Handler

    /// <summary>
    /// Handles requests for available gender options.
    /// </summary>
    public class GetGenderOptionQueryHandler : IRequestHandler<GetGenderOptionQuery, ApiResponse<List<GetGenderOptionResponseDTO>>>
    {
        #region Fields

        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetGenderOptionQueryHandler> _logger;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="GetGenderOptionQueryHandler"/> class.
        /// </summary>
        /// <param name="unitOfWork">The persistence unit of work.</param>
        /// <param name="logger">The diagnostic logger.</param>
        public GetGenderOptionQueryHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetGenderOptionQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        #endregion

        #region Handle

        /// <summary>
        /// Retrieves gender option data and constructs the successful API response.
        /// </summary>
        public async Task<ApiResponse<List<GetGenderOptionResponseDTO>>> Handle(
            GetGenderOptionQuery request,
            CancellationToken cancellationToken)
        {
            if (request?.OptionDTO == null || !request.OptionDTO.TodaysDate.HasValue)
            {
                throw new ValidationErrorException(AppConstants.ErrorMessages.RequiredDataMissing);
            }

            var genders = await _unitOfWork.GenderRepository.GetOptionAsync(request.OptionDTO);

            _logger.LogInformation("Retrieved {Count} gender options.", genders.Count);

            // Build the application response in the handler layer.
            return ApiResponse<List<GetGenderOptionResponseDTO>>.Success(
                genders,
                AppConstants.SuccessMessages.GenderOptionsRetrieved);
        }

        #endregion
    }

    #endregion
}
