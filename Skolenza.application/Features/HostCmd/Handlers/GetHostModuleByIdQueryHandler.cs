// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Retrieves a Host module after validating the current Host administrator.
// ================================================================

using skolenza.application.DTOS.Host;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.HostCmd.Handler
{
    #region Query

    /// <summary>
    /// Represents the request to retrieve one Host-scope module by identifier with an optional active-state filter.
    /// </summary>
    public class GetHostModuleByIdQuery : IRequest<ApiResponse<GetHostModuleResponseDTO>>
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="GetHostModuleByIdQuery"/> class.
        /// </summary>
        /// <param name="id">The module identifier.</param>
        /// <param name="isActive">When supplied, filters the module by its active state.</param>
        public GetHostModuleByIdQuery(int id, bool? isActive)
        {
            Id = id;
            IsActive = isActive;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the module identifier.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Gets the optional active-state filter.
        /// </summary>
        public bool? IsActive { get; }

        #endregion
    }

    #endregion

    #region Handler

    /// <summary>
    /// Handles retrieval of a single module from the Host application scope.
    /// </summary>
    public class GetHostModuleByIdQueryHandler
        : IRequestHandler<GetHostModuleByIdQuery, ApiResponse<GetHostModuleResponseDTO>>
    {
        #region Fields

        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetHostModuleByIdQueryHandler> _logger;
        private readonly ICommonRequestService _commonRequestService;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="GetHostModuleByIdQueryHandler"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work used to retrieve the Host module.</param>
        /// <param name="logger">The logger used to record handler activity.</param>
        /// <param name="commonRequestService">Validates the current Host principal.</param>
        public GetHostModuleByIdQueryHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetHostModuleByIdQueryHandler> logger,
            ICommonRequestService commonRequestService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _commonRequestService = commonRequestService;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Retrieves the requested Host-scope module.
        /// </summary>
        /// <param name="request">The query containing the module identifier and optional active-state filter.</param>
        /// <param name="cancellationToken">A token to observe while handling the query.</param>
        /// <returns>A response containing the requested Host module.</returns>
        /// <exception cref="ValidationErrorException">Thrown when the supplied identifier is not positive.</exception>
        /// <exception cref="KeyNotFoundException">Thrown when no matching Host-scope module exists.</exception>
        public async Task<ApiResponse<GetHostModuleResponseDTO>> Handle(
            GetHostModuleByIdQuery request,
            CancellationToken cancellationToken)
        {
            await _commonRequestService.ValidateHostUserRequestAsync();

            if (request.Id <= 0)
            {
                throw new ValidationErrorException("Host module Id must be greater than zero.");
            }

            _logger.LogInformation(
                "Retrieving Host module by Id {ModuleId}. IsActive: {IsActive}",
                request.Id,
                request.IsActive);

            var module = await _unitOfWork.ModuleRepository
                .GetHostModuleByIdAsync(request.Id, request.IsActive, cancellationToken);

            if (module == null)
            {
                throw new KeyNotFoundException("Host module not found.");
            }

            return ApiResponse<GetHostModuleResponseDTO>.Success(
                module,
                "Host module retrieved successfully.");
        }

        #endregion
    }

    #endregion
}
