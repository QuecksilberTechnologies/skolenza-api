// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Retrieves institution-scoped designation projections using trusted context.
// ================================================================

using skolenza.application.DTOs.Designation;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.DesignationCmd.Handlers
{
    #region Query

    /// <summary>
    /// Represents a request to retrieve designations.
    /// </summary>
    public class GetDesignationQuery : IRequest<ApiResponse<List<GetDesignationResponseDTO>>>
    {
        public GetDesignationRequestDTO DTO { get; }

        /// <summary>
        /// Initializes the listing query with client-supplied filters.
        /// </summary>
        public GetDesignationQuery(GetDesignationRequestDTO dto)
        {
            DTO = dto;
        }
    }

    #endregion

    #region Handler

    /// <summary>
    /// Handles designation listing requests for the authenticated institution.
    /// </summary>
    public class GetDesignationQueryHandler : IRequestHandler<GetDesignationQuery, ApiResponse<List<GetDesignationResponseDTO>>>
    {
        #region Fields

        private readonly IUnitOfWork _unitOfWork;
        private readonly ICommonRequestService _commonRequestService;
        private readonly ILogger<GetDesignationQueryHandler> _logger;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes the handler dependencies.
        /// </summary>
        public GetDesignationQueryHandler(
            IUnitOfWork unitOfWork,
            ICommonRequestService commonRequestService,
            ILogger<GetDesignationQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _commonRequestService = commonRequestService;
            _logger = logger;
        }

        #endregion

        #region Handle

        /// <summary>
        /// Retrieves paged designation projections without mutating the request DTO.
        /// </summary>
        public async Task<ApiResponse<List<GetDesignationResponseDTO>>> Handle(
            GetDesignationQuery request,
            CancellationToken cancellationToken)
        {
            var validation = await _commonRequestService.ValidateInstitutionUserRequestAsync();
            if (!validation.Success)
                throw new UnauthorizedAccessException(validation.ErrorMessage);

            var response = await _unitOfWork.DesignationRepository.GetAsync(
                request.DTO,
                validation.InstitutionId,
                cancellationToken);
            var data = response.Data ?? new List<GetDesignationResponseDTO>();

            _logger.LogInformation("Retrieved {Count} designations for InstitutionId: {InstitutionId}", data.Count, validation.InstitutionId);
            return new ApiResponse<List<GetDesignationResponseDTO>>
            {
                IsSucceeded = true,
                Message = data.Count == 0 ? "No records found." : "Designations retrieved successfully.",
                Data = data,
                PageNumber = response.PageNumber,
                PageSize = response.PageSize,
                TotalRecords = response.TotalCount,
                TotalPages = response.TotalPages
            };
        }

        #endregion
    }

    #endregion
}
