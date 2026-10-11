// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Soft deletes a institution-scoped department using trusted request context.
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
    #region Query

    /// <summary>
    /// Represents a request to soft delete a department.
    /// </summary>
    public class DeleteDepartmentQuery : IRequest<ApiResponse<bool>>
    {
        public DeleteDepartmentRequestDTO DTO { get; }

        /// <summary>
        /// Initializes the delete request.
        /// </summary>
        public DeleteDepartmentQuery(DeleteDepartmentRequestDTO dto)
        {
            DTO = dto;
        }
    }

    #endregion

    #region Handler

    /// <summary>
    /// Handles institution-scoped department deletion requests.
    /// </summary>
    public class DeleteDepartmentQueryHandler : IRequestHandler<DeleteDepartmentQuery, ApiResponse<bool>>
    {
        #region Fields

        private readonly IUnitOfWork _unitOfWork;
        private readonly ICommonRequestService _commonRequestService;
        private readonly ILogger<DeleteDepartmentQueryHandler> _logger;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes the handler dependencies.
        /// </summary>
        public DeleteDepartmentQueryHandler(
            IUnitOfWork unitOfWork,
            ICommonRequestService commonRequestService,
            ILogger<DeleteDepartmentQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _commonRequestService = commonRequestService;
            _logger = logger;
        }

        #endregion

        #region Handle

        /// <summary>
        /// Soft deletes a department only within the authenticated institution.
        /// </summary>
        public async Task<ApiResponse<bool>> Handle(
            DeleteDepartmentQuery request,
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
                    "Invalid Institution authorization context while deleting Department. InstitutionId: {InstitutionId}, StaffMemberId: {StaffMemberId}, TokenRoleId: {TokenRoleId}",
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

            // An inactive staffMember can be reactivated, so only a soft-deleted StaffMember
            // is safe to ignore while validating Department deletion dependencies.
            var hasStaffMembers = await _unitOfWork.DepartmentRepository
                .HasNonDeletedStaffMembersAsync(
                    request.DTO.Id,
                    institutionId,
                    cancellationToken);

            if (hasStaffMembers)
            {
                _logger.LogWarning(
                    "Department deletion blocked because non-soft-deleted staffMembers are assigned. DepartmentId: {DepartmentId}, InstitutionId: {InstitutionId}",
                    request.DTO.Id,
                    institutionId);

                throw new ConflictException(
                    AppConstants.ErrorMessages.DepartmentHasStaffMembers,
                    AppConstants.ErrorCodes.DepartmentHasStaffMemberDependencies);
            }

            var isDeleted = await _unitOfWork.DepartmentRepository.DeleteAsync(
                request.DTO.Id,
                institutionId,
                userStaffMemberId,
                cancellationToken);

            if (!isDeleted)
            {
                _logger.LogWarning("Department deletion failed. DepartmentId: {DepartmentId}", request.DTO.Id);
                throw new NotFoundException(
                    AppConstants.ErrorMessages.ResourceNotFound);
            }

            return ApiResponse<bool>.Success(true, "Department deleted successfully.");
        }

        #endregion
    }

    #endregion
}
