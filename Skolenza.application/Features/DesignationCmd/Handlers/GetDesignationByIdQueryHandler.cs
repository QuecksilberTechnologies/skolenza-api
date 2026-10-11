// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Retrieves a single designation through centralized error handling.
// ================================================================

using skolenza.application.DTOs.Designation;
using skolenza.application.DTOS.Designation;
using skolenza.application.Constants;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.DesignationCmd.Handlers
{
    #region Query

    /// <summary>
    /// Represents a request to retrieve one designation by identifier.
    /// </summary>
    public class GetDesignationByIdQuery : IRequest<ApiResponse<GetSingleDesignationResponseDTO>>
    {
        /// <summary>
        /// Gets the designation identifier request.
        /// </summary>
        public GetSingleDesignationRequestDTO Dto { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GetDesignationByIdQuery"/> class.
        /// </summary>
        /// <param name="dto">The designation identifier request.</param>
        public GetDesignationByIdQuery(GetSingleDesignationRequestDTO dto)
        {
            Dto = dto;
        }
    }

    #endregion

    #region Handler

    /// <summary>
    /// Handles retrieval of a single designation.
    /// </summary>
    public class GetDesignationByIdQueryHandler : IRequestHandler<GetDesignationByIdQuery, ApiResponse<GetSingleDesignationResponseDTO>>
    {
        #region Fields

        private readonly IUnitOfWork _unitOfWork;
        private readonly ICommonRequestService _commonRequestService;
        private readonly ILogger<GetDesignationByIdQueryHandler> _logger;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="GetDesignationByIdQueryHandler"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work used to access persistence.</param>
        /// <param name="logger">The logger used for contextual success and not-found diagnostics.</param>
        public GetDesignationByIdQueryHandler(
            IUnitOfWork unitOfWork,
            ICommonRequestService commonRequestService,
            ILogger<GetDesignationByIdQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _commonRequestService = commonRequestService;
            _logger = logger;
        }

        #endregion

        #region Handle

        /// <summary>
        /// Retrieves the requested designation or delegates its error response to middleware.
        /// </summary>
        /// <param name="request">The single-designation query.</param>
        /// <param name="cancellationToken">A token used to cancel the operation.</param>
        /// <returns>The successful designation response.</returns>
        public async Task<ApiResponse<GetSingleDesignationResponseDTO>> Handle(
            GetDesignationByIdQuery request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (request?.Dto == null || request.Dto.Id <= 0)
            {
                throw new skolenza.application.Exceptions.ValidationErrorException(
                    skolenza.application.Constants.AppConstants.ErrorMessages.InvalidIdentifier);
            }

            var validation = await _commonRequestService.ValidateInstitutionUserRequestAsync();
            if (!validation.Success || validation.InstitutionId <= 0)
            {
                throw new UnauthorizedAccessException(
                    validation.ErrorMessage ?? AppConstants.ErrorMessages.Unauthorized);
            }

            var designation = await _unitOfWork.DesignationRepository.GetByIdAsync(
                request.Dto,
                validation.InstitutionId,
                cancellationToken);

            if (designation == null)
            {
                _logger.LogWarning("No designation found with the given ID: {DesignationId}", request.Dto.Id);
                throw new skolenza.application.Exceptions.NotFoundException(
                    skolenza.application.Constants.AppConstants.ErrorMessages.ResourceNotFound);
            }

            _logger.LogInformation("Successfully retrieved designation with ID: {DesignationId}", request.Dto.Id);
            return ApiResponse<GetSingleDesignationResponseDTO>.Success(
                designation,
                "Designation fetched successfully.");
        }

        #endregion
    }

    #endregion
}
