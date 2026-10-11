// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Updates a institution-scoped department using trusted request context.
// ================================================================

using skolenza.application.Constants;
using skolenza.application.DTOs.Department;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.DepartmentCmd.Handlers
{
    #region Command

    /// <summary>
    /// Represents a request to update a department.
    /// </summary>
    public class UpdateDepartmentCommad : IRequest<ApiResponse<bool>>
    {
        public UpdateDepartmentRequestDTO DTO { get; }

        /// <summary>
        /// Initializes the update request with client-editable department values.
        /// </summary>
        public UpdateDepartmentCommad(UpdateDepartmentRequestDTO dto)
        {
            DTO = dto;
        }
    }

    #endregion

    #region Handler

    /// <summary>
    /// Handles updates to departments owned by the authenticated institution.
    /// </summary>
    public class UpdateDepartmentCommadHandler : IRequestHandler<UpdateDepartmentCommad, ApiResponse<bool>>
    {
        #region Fields

        private readonly IUnitOfWork _unitOfWork;
        private readonly ICommonRequestService _commonRequestService;
        private readonly ILogger<UpdateDepartmentCommadHandler> _logger;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes the handler dependencies.
        /// </summary>
        public UpdateDepartmentCommadHandler(
            IUnitOfWork unitOfWork,
            ICommonRequestService commonRequestService,
            ILogger<UpdateDepartmentCommadHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _commonRequestService = commonRequestService;
            _logger = logger;
        }

        #endregion

        #region Handle

        /// <summary>
        /// Updates client-editable department fields after enforcing institution ownership.
        /// </summary>
        public async Task<ApiResponse<bool>> Handle(
            UpdateDepartmentCommad request,
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
                    "Invalid Institution authorization context while updating Department. InstitutionId: {InstitutionId}, StaffMemberId: {StaffMemberId}, TokenRoleId: {TokenRoleId}",
                    institutionId,
                    userStaffMemberId,
                    tokenRoleId);

                throw new UnauthorizedAccessException(
                    AppConstants.ErrorMessages.Unauthorized);
            }

            #endregion

            if (request.DTO.Id <= 0)
                throw new ValidationErrorException(
                    AppConstants.ErrorMessages.InvalidIdentifier);

            if (string.IsNullOrWhiteSpace(request.DTO.DepartmentName))
                throw new ValidationErrorException(
                    AppConstants.ErrorMessages.InvalidRequest);

            // Load the existing institution-owned entity to retain immutable and omitted values.
            var department = await _unitOfWork.DepartmentRepository.GetByIdForInstitutionAsync(
                request.DTO.Id,
                institutionId,
                cancellationToken);

            if (department == null)
                throw new NotFoundException(
                    AppConstants.ErrorMessages.ResourceNotFound);

            // Inactive staffMembers remain a dependency because they can later be
            // reactivated. Only staffMembers already soft deleted are ignored.
            if (department.IsActive && request.DTO.IsActive == false &&
                await _unitOfWork.DepartmentRepository.HasNonDeletedStaffMembersAsync(
                    department.Id,
                    institutionId,
                    cancellationToken))
            {
                _logger.LogWarning(
                    "Department deactivation blocked because non-soft-deleted staffMembers are assigned. DepartmentId: {DepartmentId}, InstitutionId: {InstitutionId}",
                    department.Id,
                    institutionId);

                throw new ConflictException(
                    AppConstants.ErrorMessages.DepartmentHasStaffMembers,
                    AppConstants.ErrorCodes.DepartmentHasStaffMemberDependencies);
            }

            // Apply client-editable fields and server-controlled audit values.
            department.DepartmentName = request.DTO.DepartmentName.Trim();
            if (request.DTO.Description != null)
                department.Description = request.DTO.Description;
            if (request.DTO.Remark != null)
                department.Remark = request.DTO.Remark;
            if (request.DTO.IsActive.HasValue)
                department.IsActive = request.DTO.IsActive.Value;

            department.UpdatedById = userStaffMemberId;
            department.UpdatedDateTime = DateTime.UtcNow;

            // Persist the prepared domain entity.
            var isUpdated = await _unitOfWork.DepartmentRepository.UpdateAsync(department, cancellationToken);
            if (!isUpdated)
            {
                _logger.LogWarning("Department update failed. DepartmentId: {DepartmentId}", request.DTO.Id);
                throw new ConflictException(
                    AppConstants.ErrorMessages.ResourceConflict);
            }

            _logger.LogInformation("Department updated successfully. DepartmentId: {DepartmentId}", request.DTO.Id);
            return ApiResponse<bool>.Success(true, "Department updated successfully.");
        }

        #endregion
    }

    #endregion
}
