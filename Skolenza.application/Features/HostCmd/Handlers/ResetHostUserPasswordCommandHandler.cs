// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Resets a host-user password for an authorized administrator.
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
    /// Represents the request to reset a host-user password.
    /// </summary>
    public class ResetHostUserPasswordCommand : IRequest<ApiResponse<bool>>
    {
        /// <summary>
        /// Gets the password-reset details.
        /// </summary>
        public ResetHostUserPasswordRequestDTO? DTO { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResetHostUserPasswordCommand"/> class.
        /// </summary>
        /// <param name="dto">The password-reset details.</param>
        public ResetHostUserPasswordCommand(ResetHostUserPasswordRequestDTO? dto)
        {
            DTO = dto;
        }
    }

    #endregion

    #region Handler

    /// <summary>
    /// Handles administrative password resets for host users.
    /// </summary>
    public class ResetHostUserPasswordCommandHandler
        : IRequestHandler<ResetHostUserPasswordCommand, ApiResponse<bool>>
    {
        #region Fields

        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordService _passwordService;
        private readonly ILogger<ResetHostUserPasswordCommandHandler> _logger;
        private readonly ICommonRequestService _commonRequestService;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="ResetHostUserPasswordCommandHandler"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work used to retrieve and update the host user.</param>
        /// <param name="passwordService">The password service used to verify and hash passwords.</param>
        /// <param name="logger">The logger used to record handler activity.</param>
        /// <param name="commonRequestService">Validates the current Host principal.</param>
        public ResetHostUserPasswordCommandHandler(
            IUnitOfWork unitOfWork,
            IPasswordService passwordService,
            ILogger<ResetHostUserPasswordCommandHandler> logger,
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
        /// Resets the password for the requested host user without requiring the old password.
        /// </summary>
        /// <param name="request">The command containing the password-reset details.</param>
        /// <param name="cancellationToken">A token to observe while handling the command.</param>
        /// <returns>A response indicating whether the password was reset.</returns>
        public async Task<ApiResponse<bool>> Handle(
            ResetHostUserPasswordCommand request,
            CancellationToken cancellationToken)
        {
            var hostUserId = await _commonRequestService.ValidateHostUserRequestAsync();

            if (request.DTO == null)
            {
                throw new ValidationErrorException(
                    "Host user password-reset details are required.");
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
                "Reset password for host user with Id {HostUserId}.",
                hostUser.Id);

            return ApiResponse<bool>.Success(
                true,
                "Host user password reset successfully.");
        }

        #endregion
    }

    #endregion
}
