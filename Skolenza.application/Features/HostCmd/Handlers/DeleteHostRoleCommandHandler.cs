// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Soft deletes a host role by its identifier.
// ================================================================

using skolenza.application.DTOS.Host;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace skolenza.application.Features.HostCmd.Handler
{
    #region Command

    /// <summary>
    /// Represents the request to soft delete a host role.
    /// </summary>
    public class DeleteHostRoleCommand : IRequest<ApiResponse<bool>>
    {
        /// <summary>
        /// Gets the host-role identifier to delete.
        /// </summary>
        public DeleteHostRoleRequestDTO? DTO { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteHostRoleCommand"/> class.
        /// </summary>
        /// <param name="dto">The host-role identifier to delete.</param>
        public DeleteHostRoleCommand(DeleteHostRoleRequestDTO? dto)
        {
            DTO = dto;
        }
    }

    #endregion

    #region Handler

    /// <summary>
    /// Handles soft deletion of a host role.
    /// </summary>
    public class DeleteHostRoleCommandHandler
        : IRequestHandler<DeleteHostRoleCommand, ApiResponse<bool>>
    {
        #region Fields

        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeleteHostRoleCommandHandler> _logger;
        private readonly ICommonRequestService _commonRequestService;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteHostRoleCommandHandler"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work used to delete the host role.</param>
        /// <param name="logger">The logger used to record handler activity.</param>
        /// <param name="commonRequestService">Validates the current Host principal.</param>
        public DeleteHostRoleCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<DeleteHostRoleCommandHandler> logger,
            ICommonRequestService commonRequestService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _commonRequestService = commonRequestService;
        }

        #endregion

        #region Handler

        /// <summary>
        /// Soft deletes the requested host role.
        /// </summary>
        /// <param name="request">The command containing the host-role identifier.</param>
        /// <param name="cancellationToken">A token to observe while handling the command.</param>
        /// <returns>A response indicating whether the host role was soft deleted.</returns>
        public async Task<ApiResponse<bool>> Handle(
            DeleteHostRoleCommand request,
            CancellationToken cancellationToken)
        {
            var hostUserId = await _commonRequestService.ValidateHostUserRequestAsync();

            if (request.DTO == null)
            {
                throw new ValidationErrorException(
                    "Host role delete details are required.");
            }

            if (request.DTO.Id <= 0)
            {
                throw new ValidationErrorException(
                    "Host role Id must be greater than zero.");
            }

            var hostRole = await _unitOfWork.HostRoleRepository
                .GetByIdAsync(request.DTO.Id);

            if (hostRole == null)
            {
                throw new KeyNotFoundException("Host role not found.");
            }

            var hasAssignedHostUsers = await _unitOfWork.HostUserRepository
                .AnyActiveUserByHostRoleIdAsync(hostRole.Id);

            if (hasAssignedHostUsers)
            {
                throw new ApiException(
                    "Host role cannot be deleted because it is assigned to one or more host users.",
                    409);
            }

            hostRole.IsSoftDeleted = true;
            hostRole.IsActive = false;
            hostRole.DeletedById = hostUserId;
            hostRole.DeletedDateTime = DateTime.UtcNow;

            var result = await _unitOfWork.HostRoleRepository
                .DeleteAsync(hostRole);

            _logger.LogInformation(
                "Soft deleted host role with Id {HostRoleId}.",
                hostRole.Id);

            return ApiResponse<bool>.Success(
                result,
                "Host role deleted successfully.");
        }

        #endregion
    }

    #endregion
}
