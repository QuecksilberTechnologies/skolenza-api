// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Changes a host-user password after verifying the current password.
// ================================================================

using skolenza.application.DTOS.Host;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IHashed;
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
    /// Represents the request to change a host-user password.
    /// </summary>
    public class ChangeHostUserPasswordCommand : IRequest<ApiResponse<bool>>
    {
        /// <summary>
        /// Gets the password-change details.
        /// </summary>
        public ChangeHostUserPasswordRequestDTO? DTO { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeHostUserPasswordCommand"/> class.
        /// </summary>
        /// <param name="dto">The password-change details.</param>
        public ChangeHostUserPasswordCommand(ChangeHostUserPasswordRequestDTO? dto)
        {
            DTO = dto;
        }
    }

    #endregion

    #region Handler

    /// <summary>
    /// Handles password changes for host users after verifying the current password.
    /// </summary>
    public class ChangeHostUserPasswordCommandHandler
        : IRequestHandler<ChangeHostUserPasswordCommand, ApiResponse<bool>>
    {
        #region Fields

        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordService _passwordService;
        private readonly ILogger<ChangeHostUserPasswordCommandHandler> _logger;
        private readonly ICommonRequestService _commonRequestService;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeHostUserPasswordCommandHandler"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work used to retrieve and update the host user.</param>
        /// <param name="passwordService">The password service used to verify and hash passwords.</param>
        /// <param name="logger">The logger used to record handler activity.</param>
        /// <param name="commonRequestService">Validates the current Host principal.</param>
        public ChangeHostUserPasswordCommandHandler(
            IUnitOfWork unitOfWork,
            IPasswordService passwordService,
            ILogger<ChangeHostUserPasswordCommandHandler> logger,
            ICommonRequestService commonRequestService)
        {
            _unitOfWork = unitOfWork;
            _passwordService = passwordService;
            _logger = logger;
            _commonRequestService = commonRequestService;
        }

        #endregion

        #region Handler

        /// <summary>
        /// Changes the password for the requested host user.
        /// </summary>
        /// <param name="request">The command containing the password-change details.</param>
        /// <param name="cancellationToken">A token to observe while handling the command.</param>
        /// <returns>A response indicating whether the password was changed.</returns>
        public async Task<ApiResponse<bool>> Handle(
            ChangeHostUserPasswordCommand request,
            CancellationToken cancellationToken)
        {
            var hostUserId = await _commonRequestService.ValidateHostUserRequestAsync();

            if (request.DTO == null)
            {
                throw new ValidationErrorException(
                    "Host user password-change details are required.");
            }

            var dto = request.DTO;

            if (dto.HostUserId <= 0)
            {
                throw new ValidationErrorException(
                    "Host user Id must be greater than zero.");
            }

            var hostUser = await _unitOfWork.HostUserRepository
                .GetForUpdateAsync(dto.HostUserId);

            if (hostUser == null)
            {
                throw new KeyNotFoundException("Host user not found.");
            }

            if (string.IsNullOrWhiteSpace(dto.OldPassword))
            {
                throw new ValidationErrorException("Old password is required.");
            }

            if (string.IsNullOrWhiteSpace(dto.NewPassword))
            {
                throw new ValidationErrorException("New password is required.");
            }

            if (string.IsNullOrWhiteSpace(dto.ConfirmPassword))
            {
                throw new ValidationErrorException("Confirm password is required.");
            }

            if (dto.NewPassword != dto.ConfirmPassword)
            {
                throw new ValidationErrorException(
                    "New password and confirm password do not match.");
            }

            if (!_passwordService.VerifyPassword(
                    hostUser.PasswordHash,
                    dto.OldPassword))
            {
                throw new ValidationErrorException("Old password is incorrect.");
            }

            if (_passwordService.VerifyPassword(
                    hostUser.PasswordHash,
                    dto.NewPassword))
            {
                throw new ValidationErrorException(
                    "New password must be different from the current password.");
            }

            hostUser.PasswordHash = _passwordService.HashPassword(dto.NewPassword);
            hostUser.UpdatedById = hostUserId;
            hostUser.UpdatedDateTime = DateTime.UtcNow;

            await _unitOfWork.HostUserRepository.UpdateAsync(hostUser);

            _logger.LogInformation(
                "Changed password for host user with Id {HostUserId}.",
                hostUser.Id);

            return ApiResponse<bool>.Success(
                true,
                "Password changed successfully.");
        }

        #endregion
    }

    #endregion
}
