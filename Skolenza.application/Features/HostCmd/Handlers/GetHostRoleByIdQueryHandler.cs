// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Retrieves a host role by its identifier.
// ================================================================

using skolenza.application.DTOS.Host;
using skolenza.application.Exceptions;
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
    /// Represents the request to retrieve a host role by identifier.
    /// </summary>
    public class GetHostRoleByIdQuery : IRequest<ApiResponse<GetHostRoleResponseDTO>>
    {
        /// <summary>
        /// Gets the host-role identifier.
        /// </summary>
        public long Id { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GetHostRoleByIdQuery"/> class.
        /// </summary>
        /// <param name="id">The host-role identifier.</param>
        public GetHostRoleByIdQuery(long id)
        {
            Id = id;
        }
    }

    #endregion

    #region Handler

    /// <summary>
    /// Handles retrieval of a host role by identifier.
    /// </summary>
    public class GetHostRoleByIdQueryHandler
        : IRequestHandler<GetHostRoleByIdQuery, ApiResponse<GetHostRoleResponseDTO>>
    {
        #region Fields

        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetHostRoleByIdQueryHandler> _logger;
        private readonly ICommonRequestService _commonRequestService;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="GetHostRoleByIdQueryHandler"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work used to retrieve the host role.</param>
        /// <param name="logger">The logger used to record handler activity.</param>
        /// <param name="commonRequestService">Validates the current Host principal.</param>
        public GetHostRoleByIdQueryHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetHostRoleByIdQueryHandler> logger,
            ICommonRequestService commonRequestService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _commonRequestService = commonRequestService;
        }

        #endregion

        #region Handler

        /// <summary>
        /// Retrieves the requested host role.
        /// </summary>
        /// <param name="request">The query containing the host-role identifier.</param>
        /// <param name="cancellationToken">A token to observe while handling the query.</param>
        /// <returns>A response containing the requested host-role details.</returns>
        public async Task<ApiResponse<GetHostRoleResponseDTO>> Handle(
            GetHostRoleByIdQuery request,
            CancellationToken cancellationToken)
        {
            await _commonRequestService.ValidateHostUserRequestAsync();

            if (request.Id <= 0)
            {
                throw new ValidationErrorException(
                    "Host role Id must be greater than zero.");
            }

            _logger.LogInformation(
                "Retrieving host role by Id {HostRoleId}.",
                request.Id);

            var hostRole = await _unitOfWork.HostRoleRepository
                .GetByIdAsync(request.Id);

            if (hostRole == null)
            {
                throw new KeyNotFoundException("Host role not found.");
            }

            var response = new GetHostRoleResponseDTO
            {
                Id = hostRole.Id,
                Name = hostRole.Name,
                Description = hostRole.Description,
                IsActive = hostRole.IsActive,
                AddedDateTime = hostRole.AddedDateTime,
                UpdatedDateTime = hostRole.UpdatedDateTime
            };

            return ApiResponse<GetHostRoleResponseDTO>.Success(
                response,
                "Host role retrieved successfully.");
        }

        #endregion
    }

    #endregion
}
