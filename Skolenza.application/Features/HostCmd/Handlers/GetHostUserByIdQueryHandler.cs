// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Retrieves a host user by its identifier.
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
    /// Represents the request to retrieve a host user by identifier.
    /// </summary>
    public class GetHostUserByIdQuery : IRequest<ApiResponse<GetHostUserResponseDTO>>
    {
        /// <summary>
        /// Gets the host-user identifier.
        /// </summary>
        public long Id { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GetHostUserByIdQuery"/> class.
        /// </summary>
        /// <param name="id">The host-user identifier.</param>
        public GetHostUserByIdQuery(long id)
        {
            Id = id;
        }
    }

    #endregion

    #region Handler

    /// <summary>
    /// Handles retrieval of a host user by identifier.
    /// </summary>
    public class GetHostUserByIdQueryHandler
        : IRequestHandler<GetHostUserByIdQuery, ApiResponse<GetHostUserResponseDTO>>
    {
        #region Fields

        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetHostUserByIdQueryHandler> _logger;
        private readonly ICommonRequestService _commonRequestService;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="GetHostUserByIdQueryHandler"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work used to retrieve the host user.</param>
        /// <param name="logger">The logger used to record handler activity.</param>
        /// <param name="commonRequestService">Validates the current Host principal.</param>
        public GetHostUserByIdQueryHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetHostUserByIdQueryHandler> logger,
            ICommonRequestService commonRequestService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _commonRequestService = commonRequestService;
        }

        #endregion

        #region Handler

        /// <summary>
        /// Retrieves the requested host user.
        /// </summary>
        /// <param name="request">The query containing the host-user identifier.</param>
        /// <param name="cancellationToken">A token to observe while handling the query.</param>
        /// <returns>A response containing the requested host-user details.</returns>
        public async Task<ApiResponse<GetHostUserResponseDTO>> Handle(
            GetHostUserByIdQuery request,
            CancellationToken cancellationToken)
        {
            await _commonRequestService.ValidateHostUserRequestAsync();

            if (request.Id <= 0)
            {
                throw new ValidationErrorException(
                    "Host user Id must be greater than zero.");
            }

            _logger.LogInformation(
                "Retrieving host user by Id {HostUserId}.",
                request.Id);

            var hostUser = await _unitOfWork.HostUserRepository
                .GetByIdAsync(request.Id);

            if (hostUser == null)
            {
                throw new KeyNotFoundException("Host user not found.");
            }

            var response = new GetHostUserResponseDTO
            {
                Id = hostUser.Id,
                HostRoleId = hostUser.HostRoleId,
                HostRoleName = hostUser.HostRole?.Name,
                Name = hostUser.Name,
                LoginId = hostUser.LoginId,
                Email = hostUser.Email,
                MobileNumber = hostUser.MobileNumber,
                IsActive = hostUser.IsActive,
                AddedDateTime = hostUser.AddedDateTime,
                UpdatedDateTime = hostUser.UpdatedDateTime
            };

            return ApiResponse<GetHostUserResponseDTO>.Success(
                response,
                "Host user retrieved successfully.");
        }

        #endregion
    }

    #endregion
}
