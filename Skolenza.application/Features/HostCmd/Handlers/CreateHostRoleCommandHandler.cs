// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles the request to create a host role.
// ================================================================

using AutoMapper;
using skolenza.application.DTOS.Host;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IHashed;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace skolenza.application.Features.HostCmd.Handler
{
    #region Command

    /// <summary>
    /// Represents the request to create a host role from the supplied role details.
    /// </summary>
    public class CreateHostRoleCommand : IRequest<ApiResponse<CreateHostRoleResponseDTO>>
    {
        /// <summary>
        /// Gets the host-role details to create.
        /// </summary>
        public CreateHostRoleRequestDTO DTO { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateHostRoleCommand"/> class.
        /// </summary>
        /// <param name="dto">The host-role details used to create the role.</param>
        public CreateHostRoleCommand(CreateHostRoleRequestDTO dto)
        {
            DTO = dto;
        }
    }

    #endregion

    #region Handler

    /// <summary>
    /// Handles creation of a host role.
    /// </summary>
    public class CreateHostRoleCommandHandler
       : IRequestHandler<
           CreateHostRoleCommand,
           ApiResponse<CreateHostRoleResponseDTO>>
    {
        #region Fields

        private readonly IUnitOfWork _unitOfWork;
        private readonly ICommonRequestService _commonRequestService;
        private readonly ILogger<CreateHostRoleCommandHandler> _logger;
        private readonly IMapper _mapper;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateHostRoleCommandHandler"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work used for host-role persistence.</param>
        /// <param name="mapper">The mapper used to convert request and response models.</param>
        /// <param name="commonRequestService">The common request service supplied to this handler.</param>
        /// <param name="logger">The logger used to record handler failures.</param>
        /// <param name="passwordService">The password service supplied to this handler.</param>
        public CreateHostRoleCommandHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICommonRequestService commonRequestService,
            ILogger<CreateHostRoleCommandHandler> logger,
            IPasswordService passwordService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _commonRequestService = commonRequestService;
            _logger = logger;
        }

        #endregion

        #region Handler Method

        /// <summary>
        /// Creates a host role using the supplied command.
        /// </summary>
        /// <param name="request">The command containing the host-role details.</param>
        /// <param name="cancellationToken">The token used to observe cancellation.</param>
        /// <returns>A response containing the created host-role details.</returns>
        public async Task<ApiResponse<CreateHostRoleResponseDTO>> Handle(
            CreateHostRoleCommand request,
            CancellationToken cancellationToken)
        {
            var hostUserId = await _commonRequestService.ValidateHostUserRequestAsync();

            if (request?.DTO == null)
            {
                throw new ValidationErrorException("Host role details are required.");
            }

            var utcNow = DateTime.UtcNow;
            var entity = _mapper.Map<HostRole>(request.DTO);
            entity.IsActive = true;
            entity.IsSoftDeleted = false;
            entity.AddedById = hostUserId;
            entity.AddedDateTime = utcNow;
            entity.UpdatedById = null;
            entity.UpdatedDateTime = null;
            entity.DeletedById = null;
            entity.DeletedDateTime = null;

            var transactionStarted = false;
            try
            {
                await _unitOfWork.BeginTransactionAsync(cancellationToken);
                transactionStarted = true;

                // Create Host Role
                await _unitOfWork.HostRoleRepository
                    .AddAsync(entity);

                // Prepare Response
                var response =
                    _mapper.Map<CreateHostRoleResponseDTO>(entity);

                //// Get permissions assigned to this role
                //var permissions =
                //    await _unitOfWork.HostRoleRepository
                //        .GetRolePermissionsAsync(entity.Id);

                //response.Permissions = permissions;

                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                transactionStarted = false;

                return ApiResponse<CreateHostRoleResponseDTO>.Success(
                    response,
                    "Host role created successfully.");
            }
            catch (Exception ex)
            {
                if (transactionStarted)
                {
                    await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
                }

                _logger.LogError(
                    ex,
                    "Error while creating Host Role.");

                throw;
            }
        }

        #endregion
    }

    #endregion
}
