// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Creates institution roles using trusted request context.
// ================================================================

using AutoMapper;
using skolenza.application.Constants;
using skolenza.application.DTOs.Role;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.RoleCmd.Handlers
{
    #region Command

    /// <summary>
    /// Represents a request to create a institution role.
    /// </summary>
    public class CreateRoleCommand : IRequest<ApiResponse<List<GetRoleResponseDTO>>>
    {
        public CreateRoleRequestDTO DTO { get; }

        /// <summary>
        /// Initializes the create request.
        /// </summary>
        public CreateRoleCommand(CreateRoleRequestDTO dto)
        {
            DTO = dto;
        }
    }

    #endregion

    #region Handler

    /// <summary>
    /// Handles institution-role creation for the authenticated institution.
    /// </summary>
    public class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, ApiResponse<List<GetRoleResponseDTO>>>
    {
        #region Fields

        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICommonRequestService _commonRequestService;
        private readonly ILogger<CreateRoleCommandHandler> _logger;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes the handler dependencies.
        /// </summary>
        public CreateRoleCommandHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICommonRequestService commonRequestService,
            ILogger<CreateRoleCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _commonRequestService = commonRequestService;
            _logger = logger;
        }

        #endregion

        #region Handle

        /// <summary>
        /// Creates a role from client-editable values and trusted institution audit context.
        /// </summary>
        public async Task<ApiResponse<List<GetRoleResponseDTO>>> Handle(
            CreateRoleCommand request,
            CancellationToken cancellationToken)
        {
            var validation = await _commonRequestService.ValidateInstitutionUserRequestAsync();
            if (!validation.Success)
                throw new UnauthorizedAccessException(validation.ErrorMessage);

            long institutionId = validation.InstitutionId;
            long userStaffMemberId = validation.LoggedInStaffMemberId;
            int tokenRoleId = validation.RoleId;
            if (institutionId <= 0 || userStaffMemberId <= 0 || tokenRoleId <= 0)
            {
                _logger.LogWarning("Invalid Institution authorization context while creating Role. InstitutionId: {InstitutionId}, StaffMemberId: {StaffMemberId}, TokenRoleId: {TokenRoleId}", institutionId, userStaffMemberId, tokenRoleId);
                throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
            }

            var roleName = request.DTO.RoleName?.Trim();
            if (string.IsNullOrWhiteSpace(roleName))
                throw new ValidationErrorException("Role name cannot be empty.");

            if (!ConstantValues.IsSupportedInstitutionRoleType(request.DTO.RoleType))
                throw new ValidationErrorException(
                    "Role type must match an ID from the Role Type catalogue.");

            // Map client-editable fields to the domain entity.
            var entity = _mapper.Map<Role>(request.DTO);
            entity.RoleName = roleName;
            entity.InstitutionId = institutionId;
            entity.AddedById = userStaffMemberId;
            entity.AddedDateTime = DateTime.UtcNow;
            entity.IsSystemDefault = false;
            entity.IsSoftDeleted = false;

            var created = await _unitOfWork.RoleRepository.CreateAsync(entity, cancellationToken);
            if (created == null)
                throw new ApiException("This role already exists for the institution.", 409);

            _logger.LogInformation("Role created. RoleId: {RoleId}", created.Id);
            return ApiResponse<List<GetRoleResponseDTO>>.Success(
                new List<GetRoleResponseDTO> { _mapper.Map<GetRoleResponseDTO>(created) },
                "Role created successfully.");
        }

        #endregion
    }

    #endregion
}
