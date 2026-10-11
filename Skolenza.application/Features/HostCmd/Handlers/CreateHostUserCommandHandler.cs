// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles the request to create a host user.
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

namespace skolenza.application.Features.HostCmd.Handler
{
    #region Command

    /// <summary>
    /// Represents the request to create a host user from the supplied user details.
    /// </summary>
    public class CreateHostUserCommand : IRequest<ApiResponse<CreateHostUserResponseDTO>>
    {
        /// <summary>
        /// Gets the host-user details to create.
        /// </summary>
        public CreateHostUserRequestDTO DTO { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateHostUserCommand"/> class.
        /// </summary>
        /// <param name="dto">The host-user details used to create the user.</param>
        public CreateHostUserCommand(CreateHostUserRequestDTO dto)
        {
            DTO = dto;
        }
    }

    #endregion

    #region Handler

    /// <summary>
    /// Handles creation of a host user.
    /// </summary>
    public class CreateHostUserCommandHandler
        : IRequestHandler<
            CreateHostUserCommand,
            ApiResponse<CreateHostUserResponseDTO>>
    {
        #region Fields

        private readonly IUnitOfWork _unitOfWork;
        private readonly ICommonRequestService _commonRequestService;
        private readonly ILogger<CreateHostUserCommandHandler> _logger;
        private readonly IMapper _mapper;
        private readonly IPasswordService _passwordService;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateHostUserCommandHandler"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work used for host-user persistence.</param>
        /// <param name="mapper">The mapper used to convert request and response models.</param>
        /// <param name="commonRequestService">The common request service supplied to this handler.</param>
        /// <param name="logger">The logger used to record handler failures.</param>
        /// <param name="passwordService">The password service used to hash the host-user password.</param>
        public CreateHostUserCommandHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICommonRequestService commonRequestService,
            ILogger<CreateHostUserCommandHandler> logger,
            IPasswordService passwordService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _commonRequestService = commonRequestService;
            _logger = logger;
            _passwordService = passwordService;
        }

        #endregion

        #region Handler Method

        /// <summary>
        /// Creates a host user using the supplied command.
        /// </summary>
        /// <param name="request">The command containing the host-user details.</param>
        /// <param name="cancellationToken">The token used to observe cancellation.</param>
        /// <returns>A response containing the created host-user details.</returns>
        public async Task<ApiResponse<CreateHostUserResponseDTO>> Handle(
            CreateHostUserCommand request,
            CancellationToken cancellationToken)
        {
            var hostUserId = await _commonRequestService.ValidateHostUserRequestAsync();

            if (request?.DTO == null)
            {
                throw new ValidationErrorException("Host user details are required.");
            }

            var dto = request.DTO;
            var utcNow = DateTime.UtcNow;

            // The request DTO contains only client-editable values; audit values come from the trusted Host context.
            var entity = _mapper.Map<HostUser>(dto);
            entity.PasswordHash = _passwordService.HashPassword(dto.Password);
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

                // Add Host User
                await _unitOfWork.HostUserRepository.AddAsync(entity);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                //// Get Role + Permissions
                //var permissions = await _unitOfWork.HostUserRepository
                //    .GetHostUserPermissionsAsync(
                //        entity.HostRoleId);

                // Prepare Response
                var response = _mapper.Map<CreateHostUserResponseDTO>(entity);

                //response.RoleName = permissions.RoleName;

                //response.Permissions = permissions.Permissions;

                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                transactionStarted = false;

                return ApiResponse<CreateHostUserResponseDTO>
                    .Success(
                        response,
                        "Host user created successfully.");
            }
            catch (Exception ex)
            {
                if (transactionStarted)
                {
                    await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
                }

                _logger.LogError(
                    ex,
                    "Error while creating Host User.");

                throw;
            }
        }

        #endregion
    }

    #endregion
}
