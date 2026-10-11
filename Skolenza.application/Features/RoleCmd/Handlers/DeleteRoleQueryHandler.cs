// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Soft deletes institution roles using trusted request context.
// ================================================================

using skolenza.application.DTOs.Role;
using skolenza.application.Constants;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.RoleCmd.Handlers
{
    #region Query

    /// <summary>
    /// Represents a request to soft delete a institution role.
    /// </summary>
    public class DeleteRoleQuery : IRequest<ApiResponse<bool>>
    {
        public DeleteRoleRequestDTO DTO { get; }

        /// <summary>
        /// Initializes the delete request.
        /// </summary>
        public DeleteRoleQuery(DeleteRoleRequestDTO dto)
        {
            DTO = dto;
        }
    }

    #endregion

    #region Handler

    /// <summary>
    /// Handles role deletion requests for the authenticated institution.
    /// </summary>
    public class DeleteRoleQueryHandler : IRequestHandler<DeleteRoleQuery, ApiResponse<bool>>
    {
        #region Fields

        private readonly IUnitOfWork _unitOfWork;
        private readonly ICommonRequestService _commonRequestService;
        private readonly ILogger<DeleteRoleQueryHandler> _logger;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes the handler dependencies.
        /// </summary>
        public DeleteRoleQueryHandler(
            IUnitOfWork unitOfWork,
            ICommonRequestService commonRequestService,
            ILogger<DeleteRoleQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _commonRequestService = commonRequestService;
            _logger = logger;
        }

        #endregion

        #region Handle

        /// <summary>
        /// Soft deletes a role using trusted institution and actor identifiers.
        /// </summary>
        public async Task<ApiResponse<bool>> Handle(
            DeleteRoleQuery request,
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
                _logger.LogWarning("Invalid Institution authorization context while deleting Role. InstitutionId: {InstitutionId}, StaffMemberId: {StaffMemberId}, TokenRoleId: {TokenRoleId}", institutionId, userStaffMemberId, tokenRoleId);
                throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
            }

            if (request.DTO.Id <= 0)
                throw new ValidationErrorException("Invalid role identifier.");

            var role = await _unitOfWork.RoleRepository.GetByIdForInstitutionAsync(
                request.DTO.Id,
                institutionId,
                cancellationToken);
            if (role == null)
                throw new NotFoundException(AppConstants.ErrorMessages.ResourceNotFound);

            // An inactive assignment or permission can be reactivated later, so only a
            // soft-deleted dependency is safe to ignore when deleting the Role.
            var hasDependencies = await _unitOfWork.RoleRepository
                .HasNonDeletedDependenciesAsync(request.DTO.Id, institutionId, cancellationToken);

            if (hasDependencies)
            {
                _logger.LogWarning(
                    "Role deletion blocked because dependent UserRole or RoleModuleAndPermission records exist. RoleId: {RoleId}, InstitutionId: {InstitutionId}",
                    request.DTO.Id,
                    institutionId);

                throw new ConflictException(
                    AppConstants.ErrorMessages.RoleHasDependencies,
                    AppConstants.ErrorCodes.RoleHasDependencies);
            }

            var deleted = await _unitOfWork.RoleRepository.DeleteAsync(
                request.DTO.Id,
                institutionId,
                userStaffMemberId,
                cancellationToken);
            if (!deleted)
                throw new ApiException("Role not found or already deleted.", 404);

            _logger.LogInformation("Role deleted. RoleId: {RoleId}", request.DTO.Id);
            return ApiResponse<bool>.Success(true, "Role deleted successfully.");
        }

        #endregion
    }

    #endregion
}
