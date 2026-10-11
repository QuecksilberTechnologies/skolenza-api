// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Updates institution roles using trusted request context.
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
    #region Command

    /// <summary>
    /// Represents a request to update a institution role.
    /// </summary>
    public class UpdateRoleCommand : IRequest<ApiResponse<bool>>
    {
        public UpdateRoleRequestDTO DTO { get; }

        /// <summary>
        /// Initializes the update request.
        /// </summary>
        public UpdateRoleCommand(UpdateRoleRequestDTO dto)
        {
            DTO = dto;
        }
    }

    #endregion

    #region Handler

    /// <summary>
    /// Handles role updates for the authenticated institution.
    /// </summary>
    public class UpdateRoleCommandHandler : IRequestHandler<UpdateRoleCommand, ApiResponse<bool>>
    {
        #region Fields

        private readonly IUnitOfWork _unitOfWork;
        private readonly ICommonRequestService _commonRequestService;
        private readonly ILogger<UpdateRoleCommandHandler> _logger;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes the handler dependencies.
        /// </summary>
        public UpdateRoleCommandHandler(
            IUnitOfWork unitOfWork,
            ICommonRequestService commonRequestService,
            ILogger<UpdateRoleCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _commonRequestService = commonRequestService;
            _logger = logger;
        }

        #endregion

        #region Handle

        /// <summary>
        /// Updates client-editable role values while preserving institution and creation audit fields.
        /// </summary>
        public async Task<ApiResponse<bool>> Handle(
            UpdateRoleCommand request,
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
                _logger.LogWarning("Invalid Institution authorization context while updating Role. InstitutionId: {InstitutionId}, StaffMemberId: {StaffMemberId}, TokenRoleId: {TokenRoleId}", institutionId, userStaffMemberId, tokenRoleId);
                throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
            }

            if (request.DTO.Id <= 0 || string.IsNullOrWhiteSpace(request.DTO.RoleName))
                throw new ValidationErrorException("A valid role identifier and name are required.");

            var entity = await _unitOfWork.RoleRepository.GetByIdForInstitutionAsync(
                request.DTO.Id,
                institutionId,
                cancellationToken);
            if (entity == null)
                throw new ApiException("Role not found.", 404);

            // Inactive assignments and permissions are still dependencies: both
            // can be reactivated. Only soft-deleted rows are safe to ignore.
            if (entity.IsActive && request.DTO.IsActive == false &&
                await _unitOfWork.RoleRepository.HasNonDeletedDependenciesAsync(
                    entity.Id,
                    institutionId,
                    cancellationToken))
            {
                _logger.LogWarning(
                    "Role deactivation blocked because dependent UserRole or RoleModuleAndPermission records exist. RoleId: {RoleId}, InstitutionId: {InstitutionId}",
                    entity.Id,
                    institutionId);

                throw new ConflictException(
                    AppConstants.ErrorMessages.RoleHasDependencies,
                    AppConstants.ErrorCodes.RoleHasDependencies);
            }

            entity.RoleName = request.DTO.RoleName.Trim();
            entity.RoleType = request.DTO.RoleType;
            entity.Remark = request.DTO.Remark;
            if (request.DTO.IsActive.HasValue)
                entity.IsActive = request.DTO.IsActive.Value;
            entity.UpdatedById = userStaffMemberId;
            entity.UpdatedDateTime = DateTime.UtcNow;

            var updated = await _unitOfWork.RoleRepository.UpdateAsync(entity, cancellationToken);
            if (!updated)
                throw new ApiException("Role was not updated.", 409);

            _logger.LogInformation("Role updated. RoleId: {RoleId}", entity.Id);
            return ApiResponse<bool>.Success(true, "Role updated successfully.");
        }

        #endregion
    }

    #endregion
}
