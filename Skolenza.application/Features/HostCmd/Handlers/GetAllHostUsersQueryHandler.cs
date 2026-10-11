// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Retrieves filtered and paginated Host users after validating the current Host administrator.
// ================================================================

using skolenza.application.DTOS.Host;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace skolenza.application.Features.HostCmd.Handler
{
    #region Query

    /// <summary>
    /// Represents the request to retrieve filtered and paginated Host users.
    /// </summary>
    public class GetAllHostUsersQuery
        : IRequest<PagedApiResponse<GetHostUserResponseDTO>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GetAllHostUsersQuery"/> class.
        /// </summary>
        /// <param name="isActive">When supplied, filters users by active status.</param>
        /// <param name="pageNumber">The requested one-based page number.</param>
        /// <param name="pageSize">The requested number of rows per page.</param>
        public GetAllHostUsersQuery(bool? isActive, int pageNumber, int pageSize)
        {
            IsActive = isActive;
            PageNumber = pageNumber;
            PageSize = pageSize;
        }

        /// <summary>
        /// Gets the optional active-status filter.
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
    }

    #endregion

    #region Handler

    /// <summary>
    /// Handles retrieval of filtered and paginated Host users for an authenticated Host administrator.
    /// </summary>
    public class GetAllHostUsersQueryHandler
        : IRequestHandler<
            GetAllHostUsersQuery,
            PagedApiResponse<GetHostUserResponseDTO>>
    {
        #region Fields

        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetAllHostUsersQueryHandler> _logger;
        private readonly ICommonRequestService _commonRequestService;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="GetAllHostUsersQueryHandler"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work used to retrieve host users.</param>
        /// <param name="logger">The logger used to record handler activity.</param>
        /// <param name="commonRequestService">Validates the current Host principal.</param>
        public GetAllHostUsersQueryHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetAllHostUsersQueryHandler> logger,
            ICommonRequestService commonRequestService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _commonRequestService = commonRequestService;
        }

        #endregion

        #region Handler

        /// <summary>
        /// Retrieves a filtered, database-paged Host-user result.
        /// </summary>
        /// <param name="request">The query to handle.</param>
        /// <param name="cancellationToken">A token to observe while handling the query.</param>
        /// <returns>A response containing the requested non-soft-deleted Host-user page.</returns>
        public async Task<PagedApiResponse<GetHostUserResponseDTO>> Handle(
            GetAllHostUsersQuery request,
            CancellationToken cancellationToken)
        {
            // Validate the current Host identity before reading management data.
            await _commonRequestService.ValidateHostUserRequestAsync();

            var pageNumber = request.PageNumber > 0 ? request.PageNumber : 1;
            var pageSize = request.PageSize > 0 ? request.PageSize : 10;

            _logger.LogInformation("Retrieving Host users. IsActive: {IsActive}; PageNumber: {PageNumber}; PageSize: {PageSize}", request.IsActive, pageNumber, pageSize);

            var response = await _unitOfWork.HostUserRepository
                .GetPagedAsync(request.IsActive, pageNumber, pageSize, cancellationToken);

            return PagedApiResponse<GetHostUserResponseDTO>.Success(
                response,
                "Host users retrieved successfully.");
        }

        #endregion
    }

    #endregion
}
