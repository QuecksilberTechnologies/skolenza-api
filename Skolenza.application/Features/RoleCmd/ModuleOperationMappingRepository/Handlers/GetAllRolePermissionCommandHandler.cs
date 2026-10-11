// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles retrieval of module-operation permissions for a role.
// ================================================================

using skolenza.application.Constants;
using skolenza.application.DTOs.RoleModulePermission;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.RoleCmd.ModuleOperationMappingRepository.Handlers
{
    #region Query

    /// <summary>
    /// Represents a request to retrieve module-operation permissions for a role.
    /// </summary>
    public class GetRolePermissionCommand : IRequest<ApiResponse<List<RoleModuleAndPermission>>>
    {
        public GetAllActiveRoleModuleOperationsRequestByRoleIdDTO DTO { get; set; } = default!;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetRolePermissionCommand"/> class.
        /// </summary>
        public GetRolePermissionCommand(GetAllActiveRoleModuleOperationsRequestByRoleIdDTO dto)
        {
            DTO = dto;
        }
    }

    #endregion

    #region Handler

    /// <summary>
    /// Retrieves module-operation permissions for a validated institution role.
    /// </summary>
    public class GetRolePermissionCommandHandler
        : IRequestHandler<GetRolePermissionCommand, ApiResponse<List<RoleModuleAndPermission>>>
    {
        #region Fields

        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetRolePermissionCommandHandler> _logger;
        private readonly ICommonRequestService _commonRequestService;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="GetRolePermissionCommandHandler"/> class.
        /// </summary>
        public GetRolePermissionCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetRolePermissionCommandHandler> logger,
            ICommonRequestService commonRequestService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _commonRequestService = commonRequestService;
        }

        #endregion

        #region Handle

        /// <summary>
        /// Retrieves the existing module-operation assignments for the requested role.
        /// </summary>
        public async Task<ApiResponse<List<RoleModuleAndPermission>>> Handle(
            GetRolePermissionCommand request,
            CancellationToken cancellationToken)
        {
            var validation = await _commonRequestService.ValidateInstitutionUserRequestAsync();
            if (!validation.Success)
            {
                throw new UnauthorizedAccessException(validation.ErrorMessage);
            }

            if (request?.DTO == null || request.DTO.RoleId <= 0)
            {
                throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
            }

            var role = await _unitOfWork.RoleRepository.GetByIdForInstitutionAsync(
                request.DTO.RoleId,
                validation.InstitutionId,
                cancellationToken);
            if (role == null)
            {
                throw new NotFoundException(AppConstants.ErrorMessages.ResourceNotFound);
            }

            var permissions = await _unitOfWork
                .UserRolesPermissionOnModuleRepository
                .GetByRoleIdAsync(
                    request.DTO.RoleId,
                    validation.InstitutionId,
                    cancellationToken);

            _logger.LogInformation(
                "Retrieved {Count} role permissions for institution {InstitutionId}, role {RoleId}.",
                permissions.Count,
                validation.InstitutionId,
                request.DTO.RoleId);

            return ApiResponse<List<RoleModuleAndPermission>>.Success(
                permissions,
                AppConstants.SuccessMessages.RolePermissionsRetrieved);
        }

        #endregion
    }

    #endregion
}
