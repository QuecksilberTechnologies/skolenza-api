// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Handles authenticated requests for institution role option projections.
// ================================================================

using skolenza.application.Constants;
using skolenza.application.DTOS.Role;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.RoleCmd.Handlers
{
    #region Query

    /// <summary>
    /// Represents a request for role options in the authenticated institution context.
    /// </summary>
    public class GetRoleOptionQuery : IRequest<ApiResponse<List<GetRoleOptionResponseDTO>>>
    {
        public GetRoleOptionRequestDTO OptionDTO { get; set; } = default!;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetRoleOptionQuery"/> class.
        /// </summary>
        /// <param name="optionDTO">The role option query criteria.</param>
        public GetRoleOptionQuery(GetRoleOptionRequestDTO optionDTO)
        {
            OptionDTO = optionDTO;
        }
    }

    #endregion

    #region Handler

    /// <summary>
    /// Handles authenticated institution requests for role option projections.
    /// </summary>
    public class GetRoleOptionQueryHandler : IRequestHandler<GetRoleOptionQuery, ApiResponse<List<GetRoleOptionResponseDTO>>>
    {
        #region Fields

        private readonly IUnitOfWork _unitOfWork;
        private readonly ICommonRequestService _commonRequestService;
        private readonly ILogger<GetRoleOptionQueryHandler> _logger;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="GetRoleOptionQueryHandler"/> class.
        /// </summary>
        /// <param name="unitOfWork">The persistence unit of work.</param>
        /// <param name="commonRequestService">The authenticated request validator.</param>
        /// <param name="logger">The diagnostic logger.</param>
        public GetRoleOptionQueryHandler(
            IUnitOfWork unitOfWork,
            ICommonRequestService commonRequestService,
            ILogger<GetRoleOptionQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _commonRequestService = commonRequestService;
            _logger = logger;
        }

        #endregion

        #region Handle

        /// <summary>
        /// Retrieves institution role options and constructs the successful API response.
        /// </summary>
        public async Task<ApiResponse<List<GetRoleOptionResponseDTO>>> Handle(
            GetRoleOptionQuery request,
            CancellationToken cancellationToken)
        {
            var validation = await _commonRequestService.ValidateInstitutionUserRequestAsync();
            if (!validation.Success)
            {
                throw new UnauthorizedAccessException(validation.ErrorMessage);
            }

            if (request?.OptionDTO == null)
            {
                throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
            }

            var roles = await _unitOfWork.RoleRepository.GetOptionAsync(
                validation.InstitutionId,
                request.OptionDTO);

            _logger.LogInformation(
                "Retrieved {Count} role options for institution {InstitutionId}.",
                roles.Count,
                validation.InstitutionId);

            // Build the application response in the handler layer.
            return ApiResponse<List<GetRoleOptionResponseDTO>>.Success(
                roles,
                AppConstants.SuccessMessages.RoleOptionsRetrieved);
        }

        #endregion
    }

    #endregion
}
