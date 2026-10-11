// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Updates institution-scoped designations using trusted request context.
// ================================================================

using skolenza.application.Constants;
using skolenza.application.DTOs.Designation;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.DesignationCmd.Handlers
{
    #region Command

    /// <summary>
    /// Represents a request to update a designation.
    /// </summary>
    public class UpdateDesignationCommand : IRequest<ApiResponse<bool>>
    {
        public UpdateDesignationRequestDTO DTO { get; }

        /// <summary>
        /// Initializes the update request.
        /// </summary>
        public UpdateDesignationCommand(UpdateDesignationRequestDTO dto)
        {
            DTO = dto;
        }
    }

    #endregion

    #region Handler

    /// <summary>
    /// Handles updates to designations owned by the authenticated institution.
    /// </summary>
    public class UpdateDesignationCommandHandler : IRequestHandler<UpdateDesignationCommand, ApiResponse<bool>>
    {
        #region Fields

        private readonly IUnitOfWork _unitOfWork;
        private readonly ICommonRequestService _commonRequestService;
        private readonly ILogger<UpdateDesignationCommandHandler> _logger;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes the handler dependencies.
        /// </summary>
        public UpdateDesignationCommandHandler(
            IUnitOfWork unitOfWork,
            ICommonRequestService commonRequestService,
            ILogger<UpdateDesignationCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _commonRequestService = commonRequestService;
            _logger = logger;
        }

        #endregion

        #region Handle

        /// <summary>
        /// Updates client-editable fields while retaining institution and creation audit values.
        /// </summary>
        public async Task<ApiResponse<bool>> Handle(
            UpdateDesignationCommand request,
            CancellationToken cancellationToken)
        {
            #region Institution Request Validation

            var validation = await _commonRequestService.ValidateInstitutionUserRequestAsync();
            if (!validation.Success)
            {
                throw new UnauthorizedAccessException(
                    validation.ErrorMessage ??
                    AppConstants.ErrorMessages.Unauthorized);
            }

            #endregion

            #region Trusted Request Context

            long userStaffMemberId = validation.LoggedInStaffMemberId;
            long institutionId = validation.InstitutionId;
            int tokenRoleId = validation.RoleId;

            if (userStaffMemberId <= 0 || institutionId <= 0 || tokenRoleId <= 0)
            {
                _logger.LogWarning(
                    "Invalid Institution authorization context while updating Designation. InstitutionId: {InstitutionId}, StaffMemberId: {StaffMemberId}, TokenRoleId: {TokenRoleId}",
                    institutionId,
                    userStaffMemberId,
                    tokenRoleId);

                throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
            }

            #endregion

            if (request.DTO.Id <= 0)
                throw new ValidationErrorException(
                    AppConstants.ErrorMessages.InvalidIdentifier);

            var entity = await _unitOfWork.DesignationRepository.GetByIdForInstitutionAsync(
                request.DTO.Id,
                institutionId,
                cancellationToken);
            if (entity == null)
                throw new NotFoundException(
                    AppConstants.ErrorMessages.ResourceNotFound);

            // Inactive staffMembers remain a dependency because they can later be
            // reactivated. Only staffMembers already soft deleted are ignored.
            if (entity.IsActive && request.DTO.IsActive == false &&
                await _unitOfWork.DesignationRepository.HasNonDeletedStaffMembersAsync(
                    entity.Id,
                    institutionId,
                    cancellationToken))
            {
                _logger.LogWarning(
                    "Designation deactivation blocked because non-soft-deleted staffMembers are assigned. DesignationId: {DesignationId}, InstitutionId: {InstitutionId}",
                    entity.Id,
                    institutionId);

                throw new ConflictException(
                    AppConstants.ErrorMessages.DesignationHasStaffMembers,
                    AppConstants.ErrorCodes.DesignationHasStaffMemberDependencies);
            }

            // A designation name is unique within its department, not across the institution.
            var targetName = string.IsNullOrWhiteSpace(request.DTO.DesignationName)
                ? entity.DesignationName
                : request.DTO.DesignationName.Trim();
            var targetDepartmentId = request.DTO.DepartmentId > 0
                ? request.DTO.DepartmentId
                : entity.DepartmentId;
            if (await _unitOfWork.DesignationRepository.CheckDuplicateValueAsync(
                    institutionId,
                    targetDepartmentId,
                    targetName,
                    entity.Id,
                    cancellationToken))
            {
                throw new ConflictException(AppConstants.ErrorMessages.ResourceConflict);
            }

            if (!string.IsNullOrWhiteSpace(request.DTO.DesignationName))
                entity.DesignationName = request.DTO.DesignationName.Trim();
            if (request.DTO.DepartmentId > 0)
                entity.DepartmentId = request.DTO.DepartmentId;
            if (request.DTO.Description != null)
                entity.Description = request.DTO.Description;
            if (request.DTO.IsActive.HasValue)
                entity.IsActive = request.DTO.IsActive.Value;

            entity.UpdatedById = userStaffMemberId;
            entity.UpdatedDateTime = DateTime.UtcNow;

            var updated = await _unitOfWork.DesignationRepository.UpdateDesignationAsync(entity, cancellationToken);
            if (!updated)
                throw new ConflictException(
                    AppConstants.ErrorMessages.ResourceConflict);

            _logger.LogInformation("Designation updated. DesignationId: {DesignationId}", entity.Id);
            return ApiResponse<bool>.Success(true, "Designation updated successfully.");
        }

        #endregion
    }

    #endregion
}
