// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Soft deletes a host user by its identifier.
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
    /// Represents the request to soft delete a host user.
    /// </summary>
    public class DeleteHostUserCommand : IRequest<ApiResponse<bool>>
    {
        /// <summary>
        /// Gets the host-user identifier to delete.
        /// </summary>
        public DeleteHostUserRequestDTO? DTO { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteHostUserCommand"/> class.
        /// </summary>
        /// <param name="dto">The host-user identifier to delete.</param>
        public DeleteHostUserCommand(DeleteHostUserRequestDTO? dto)
        {
            DTO = dto;
        }
    }

    #endregion

    #region Handler

    /// <summary>
    /// Handles soft deletion of a host user.
    /// </summary>
    public class DeleteHostUserCommandHandler
        : IRequestHandler<DeleteHostUserCommand, ApiResponse<bool>>
    {
        #region Fields

        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeleteHostUserCommandHandler> _logger;
        private readonly ICommonRequestService _commonRequestService;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteHostUserCommandHandler"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work used to persist the soft delete.</param>
        /// <param name="logger">The logger used to record handler activity.</param>
        /// <param name="commonRequestService">Validates the current Host principal.</param>
        public DeleteHostUserCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<DeleteHostUserCommandHandler> logger,
            ICommonRequestService commonRequestService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _commonRequestService = commonRequestService;
        }

        #endregion

        #region Handler

        /// <summary>
        /// Soft deletes the requested host user.
        /// </summary>
        /// <param name="request">The command containing the host-user identifier.</param>
        /// <param name="cancellationToken">A token to observe while handling the command.</param>
        /// <returns>A response indicating whether the host user was soft deleted.</returns>
        public async Task<ApiResponse<bool>> Handle(
            DeleteHostUserCommand request,
            CancellationToken cancellationToken)
        {
            var hostUserId = await _commonRequestService.ValidateHostUserRequestAsync();

            if (request.DTO == null)
            {
                throw new ValidationErrorException(
                    "Host user delete details are required.");
            }

            if (request.DTO.Id <= 0)
            {
                throw new ValidationErrorException(
                    "Host user Id must be greater than zero.");
            }

            var hostUser = await _unitOfWork.HostUserRepository
                .GetForUpdateAsync(request.DTO.Id);

            if (hostUser == null)
            {
                throw new KeyNotFoundException("Host user not found.");
            }

            hostUser.IsSoftDeleted = true;
            hostUser.IsActive = false;
            hostUser.DeletedById = hostUserId;
            hostUser.DeletedDateTime = DateTime.UtcNow;

            var result = await _unitOfWork.HostUserRepository
                .DeleteAsync(hostUser);

            _logger.LogInformation(
                "Soft deleted host user with Id {HostUserId}.",
                hostUser.Id);

            return ApiResponse<bool>.Success(
                result,
                "Host user deleted successfully.");
        }

        #endregion
    }

    #endregion
}
