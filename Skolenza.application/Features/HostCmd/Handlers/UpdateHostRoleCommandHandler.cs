// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Updates editable host-role details.
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
    /// Represents the request to update editable host-role details.
    /// </summary>
    public class UpdateHostRoleCommand
        : IRequest<ApiResponse<UpdateHostRoleResponseDTO>>
    {
        /// <summary>
        /// Gets the host-role details to update.
        /// </summary>
        public UpdateHostRoleRequestDTO? DTO { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateHostRoleCommand"/> class.
        /// </summary>
        /// <param name="dto">The host-role details to update.</param>
        public UpdateHostRoleCommand(UpdateHostRoleRequestDTO? dto)
        {
            DTO = dto;
        }
    }

    #endregion

    #region Handler

    /// <summary>
    /// Handles updates to editable host-role details.
    /// </summary>
    public class UpdateHostRoleCommandHandler
        : IRequestHandler<
            UpdateHostRoleCommand,
            ApiResponse<UpdateHostRoleResponseDTO>>
    {
        #region Fields

        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UpdateHostRoleCommandHandler> _logger;
        private readonly ICommonRequestService _commonRequestService;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateHostRoleCommandHandler"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work used to update the host role.</param>
        /// <param name="logger">The logger used to record handler activity.</param>
        /// <param name="commonRequestService">Validates the current Host principal.</param>
        public UpdateHostRoleCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<UpdateHostRoleCommandHandler> logger,
            ICommonRequestService commonRequestService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _commonRequestService = commonRequestService;
        }

        #endregion

        #region Handler

        /// <summary>
        /// Updates the requested host role.
        /// </summary>
        /// <param name="request">The command containing the host-role details.</param>
        /// <param name="cancellationToken">A token to observe while handling the command.</param>
        /// <returns>A response containing the updated host-role details.</returns>
        public async Task<ApiResponse<UpdateHostRoleResponseDTO>> Handle(
            UpdateHostRoleCommand request,
            CancellationToken cancellationToken)
        {
            var hostUserId = await _commonRequestService.ValidateHostUserRequestAsync();

            if (request.DTO == null)
            {
                throw new ValidationErrorException(
                    "Host role update details are required.");
            }

            var dto = request.DTO;

            if (dto.Id <= 0)
            {
                throw new ValidationErrorException(
                    "Host role Id must be greater than zero.");
            }

            var hostRole = await _unitOfWork.HostRoleRepository
                .GetByIdAsync(dto.Id);

            if (hostRole == null)
            {
                throw new KeyNotFoundException("Host role not found.");
            }

            var existingRoleNameOwner = await _unitOfWork.HostRoleRepository
                .GetByRoleNameAsync(dto.Name);

            if (existingRoleNameOwner != null &&
                existingRoleNameOwner.Id != hostRole.Id)
            {
                throw new ApiException(
                    "A host role with this Name already exists.",
                    409);
            }

            hostRole.Name = dto.Name;
            hostRole.Description = dto.Description;
            hostRole.IsActive = dto.IsActive;
            hostRole.UpdatedById = hostUserId;
            hostRole.UpdatedDateTime = DateTime.UtcNow;

            var updatedHostRole = await _unitOfWork.HostRoleRepository
                .UpdateAsync(hostRole);

            _logger.LogInformation(
                "Updated host role with Id {HostRoleId}.",
                updatedHostRole.Id);

            var response = new UpdateHostRoleResponseDTO
            {
                Id = updatedHostRole.Id,
                Name = updatedHostRole.Name,
                Description = updatedHostRole.Description,
                IsActive = updatedHostRole.IsActive,
                UpdatedDateTime = updatedHostRole.UpdatedDateTime
            };

            return ApiResponse<UpdateHostRoleResponseDTO>.Success(
                response,
                "Host role updated successfully.");
        }

        #endregion
    }

    #endregion
}
