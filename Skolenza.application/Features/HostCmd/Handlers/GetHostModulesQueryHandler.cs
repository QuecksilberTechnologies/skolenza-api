// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Retrieves filtered and paginated Host modules after validating the current Host administrator.
// ================================================================

using skolenza.application.DTOS.Host;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.HostCmd.Handler
{
    #region Query

    /// <summary>
    /// Represents the request to retrieve filtered and paginated Host-scope modules.
    /// </summary>
    public class GetHostModulesQuery : IRequest<PagedApiResponse<GetHostModuleResponseDTO>>
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="GetHostModulesQuery"/> class.
        /// </summary>
        /// <param name="isActive">When supplied, filters Host modules by their active state.</param>
        /// <param name="pageNumber">The requested one-based page number.</param>
        /// <param name="pageSize">The requested number of rows per page.</param>
        public GetHostModulesQuery(bool? isActive, int pageNumber, int pageSize)
        {
            IsActive = isActive;
            PageNumber = pageNumber;
            PageSize = pageSize;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the optional active-state filter.
        /// </summary>
        public bool? IsActive { get; }

        /// <summary>
        /// Gets the requested one-based page number.
        /// </summary>
        public int PageNumber { get; }

        /// <summary>
        /// Gets the requested number of rows per page.
        /// </summary>
        public int PageSize { get; }

        #endregion
    }

    #endregion

    #region Handler

    /// <summary>
    /// Handles retrieval of modules that belong to the Host application scope.
    /// </summary>
    public class GetHostModulesQueryHandler
        : IRequestHandler<GetHostModulesQuery, PagedApiResponse<GetHostModuleResponseDTO>>
    {
        #region Fields

        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetHostModulesQueryHandler> _logger;
        private readonly ICommonRequestService _commonRequestService;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="GetHostModulesQueryHandler"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work used to retrieve modules.</param>
        /// <param name="logger">The logger used to record handler activity.</param>
        /// <param name="commonRequestService">Validates the current Host principal.</param>
        public GetHostModulesQueryHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetHostModulesQueryHandler> logger,
            ICommonRequestService commonRequestService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _commonRequestService = commonRequestService;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Retrieves Host-scope modules and returns a successful response even when none match the filter.
        /// </summary>
        /// <param name="request">The query containing the active-state filter and paging request.</param>
        /// <param name="cancellationToken">A token to observe while handling the query.</param>
        /// <returns>A response containing the matching Host-module page.</returns>
        public async Task<PagedApiResponse<GetHostModuleResponseDTO>> Handle(
            GetHostModulesQuery request,
            CancellationToken cancellationToken)
        {
            // Validate the current Host identity before reading management data.
            await _commonRequestService.ValidateHostUserRequestAsync();

            var pageNumber = request.PageNumber > 0 ? request.PageNumber : 1;
            var pageSize = request.PageSize > 0 ? request.PageSize : 10;

            _logger.LogInformation("Retrieving Host modules. IsActive: {IsActive}; PageNumber: {PageNumber}; PageSize: {PageSize}", request.IsActive, pageNumber, pageSize);

            var modules = await _unitOfWork.ModuleRepository
                .GetHostModulesAsync(request.IsActive, pageNumber, pageSize, cancellationToken);

            return PagedApiResponse<GetHostModuleResponseDTO>.Success(
                modules,
                "Host modules retrieved successfully.");
        }

        #endregion
    }

    #endregion
}
