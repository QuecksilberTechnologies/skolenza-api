// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Retrieves institution-scoped designation options using trusted context.
// ================================================================

using skolenza.application.DTOS.Designation;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.DesignationCmd.Handlers
{
    #region Query

    /// <summary>
    /// Represents a request to retrieve active designation options.
    /// </summary>
    public class GetDesignationOptionQuery : IRequest<ApiResponse<List<GetDesignationOptionResponseDTO>>>
    {
        public GetDesignationOptionRequestDTO OptionDTO { get; }

        /// <summary>
        /// Initializes the option query with client-supplied filters.
        /// </summary>
        public GetDesignationOptionQuery(GetDesignationOptionRequestDTO optionDTO)
        {
            OptionDTO = optionDTO;
        }
    }

    #endregion

    #region Handler

    /// <summary>
    /// Handles active designation-option queries for the authenticated institution.
    /// </summary>
    public class GetDesignationOptionQueryHandler : IRequestHandler<GetDesignationOptionQuery, ApiResponse<List<GetDesignationOptionResponseDTO>>>
    {
        #region Fields

        private readonly IUnitOfWork _unitOfWork;
        private readonly ICommonRequestService _commonRequestService;
        private readonly ILogger<GetDesignationOptionQueryHandler> _logger;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes the handler dependencies.
        /// </summary>
        public GetDesignationOptionQueryHandler(
            IUnitOfWork unitOfWork,
            ICommonRequestService commonRequestService,
            ILogger<GetDesignationOptionQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _commonRequestService = commonRequestService;
            _logger = logger;
        }

        #endregion

        #region Handle

        /// <summary>
        /// Retrieves active options without using the request DTO as trusted context transport.
        /// </summary>
        public async Task<ApiResponse<List<GetDesignationOptionResponseDTO>>> Handle(
            GetDesignationOptionQuery request,
            CancellationToken cancellationToken)
        {
            var validation = await _commonRequestService.ValidateInstitutionUserRequestAsync();
            if (!validation.Success)
                throw new UnauthorizedAccessException(validation.ErrorMessage);

            var options = await _unitOfWork.DesignationRepository.GetOptionAsync(
                request.OptionDTO.DepartmentId,
                validation.InstitutionId,
                cancellationToken);

            _logger.LogInformation("Retrieved {Count} designation options for InstitutionId: {InstitutionId}", options.Count, validation.InstitutionId);
            return ApiResponse<List<GetDesignationOptionResponseDTO>>.Success(
                options,
                options.Count == 0 ? "No designations found for this institution." : "Designation options fetched successfully.");
        }

        #endregion
    }

    #endregion
}
