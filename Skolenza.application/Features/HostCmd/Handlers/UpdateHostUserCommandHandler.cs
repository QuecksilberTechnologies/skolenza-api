// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Updates editable host-user details without changing credentials.
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
    /// Represents the request to update editable host-user details.
    /// </summary>
    public class UpdateHostUserCommand
        : IRequest<ApiResponse<UpdateHostUserResponseDTO>>
    {
        /// <summary>
        /// Gets the host-user details to update.
        /// </summary>
        public UpdateHostUserRequestDTO? DTO { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateHostUserCommand"/> class.
        /// </summary>
        /// <param name="dto">The host-user details to update.</param>
        public UpdateHostUserCommand(UpdateHostUserRequestDTO? dto)
        {
            DTO = dto;
        }
    }

    #endregion

    #region Handler

    /// <summary>
    /// Handles updates to editable host-user details.
    /// </summary>
    public class UpdateHostUserCommandHandler
        : IRequestHandler<
            UpdateHostUserCommand,
            ApiResponse<UpdateHostUserResponseDTO>>
    {
        #region Fields

        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UpdateHostUserCommandHandler> _logger;
        private readonly ICommonRequestService _commonRequestService;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateHostUserCommandHandler"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work used to update the host user.</param>
        /// <param name="logger">The logger used to record handler activity.</param>
        /// <param name="commonRequestService">Validates the current Host principal.</param>
        public UpdateHostUserCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<UpdateHostUserCommandHandler> logger,
            ICommonRequestService commonRequestService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _commonRequestService = commonRequestService;
        }

        #endregion

        #region Handler

        /// <summary>
        /// Updates the requested host user.
        /// </summary>
        /// <param name="request">The command containing the host-user details.</param>
        /// <param name="cancellationToken">A token to observe while handling the command.</param>
        /// <returns>A response containing the updated host-user details.</returns>
        public async Task<ApiResponse<UpdateHostUserResponseDTO>> Handle(
            UpdateHostUserCommand request,
            CancellationToken cancellationToken)
        {
            var hostUserId = await _commonRequestService.ValidateHostUserRequestAsync();

            if (request.DTO == null)
            {
                throw new ValidationErrorException(
                    "Host user update details are required.");
            }

            var dto = request.DTO;

            {
            if (dto.Id <= 0)
                throw new ValidationErrorException(
                    "Host user Id must be greater than zero.");
            }

            var hostUser = await _unitOfWork.HostUserRepository
                .GetForUpdateAsync(dto.Id);

            if (hostUser == null)
            {
                throw new KeyNotFoundException("Host user not found.");
            }

            var existingLoginIdOwner = await _unitOfWork.HostUserRepository
                .GetByLoginIdAsync(dto.LoginId);

            if (existingLoginIdOwner != null &&
                existingLoginIdOwner.Id != hostUser.Id)
            {
                throw new ApiException(
                    "A host user with this LoginId already exists.",
                    409);
            }

            var transactionStarted = false;
            try
            {
                await _unitOfWork.BeginTransactionAsync(cancellationToken);
                transactionStarted = true;

                hostUser.HostRoleId = dto.HostRoleId;
                hostUser.Name = dto.Name;
                hostUser.LoginId = dto.LoginId;
                hostUser.Email = dto.Email;
                hostUser.MobileNumber = dto.MobileNumber;
                hostUser.IsActive = dto.IsActive;
                hostUser.UpdatedById = hostUserId;
                hostUser.UpdatedDateTime = DateTime.UtcNow;

                var updatedHostUser = await _unitOfWork.HostUserRepository
                    .UpdateAsync(hostUser);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                transactionStarted = false;

                _logger.LogInformation(
                    "Updated host user with Id {HostUserId}.",
                    updatedHostUser.Id);

                var response = new UpdateHostUserResponseDTO
                {
                    Id = updatedHostUser.Id,
                    HostRoleId = updatedHostUser.HostRoleId,
                    HostRoleName = updatedHostUser.HostRole?.Name,
                    Name = updatedHostUser.Name,
                    LoginId = updatedHostUser.LoginId,
                    Email = updatedHostUser.Email,
                    MobileNumber = updatedHostUser.MobileNumber,
                    IsActive = updatedHostUser.IsActive,
                    UpdatedDateTime = updatedHostUser.UpdatedDateTime
                };

                return ApiResponse<UpdateHostUserResponseDTO>.Success(
                    response,
                    "Host user updated successfully.");
            }
            catch
            {
                if (transactionStarted)
                {
                    await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
                }

                throw;
            }
        }

        #endregion
    }

    #endregion
}
