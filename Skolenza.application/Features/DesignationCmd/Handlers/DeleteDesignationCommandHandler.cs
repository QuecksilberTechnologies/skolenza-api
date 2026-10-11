// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Soft deletes institution-scoped designations using trusted request context.
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
    #region Query

    /// <summary>
    /// Represents a request to soft delete a designation.
    /// </summary>
    public class DeleteDesignationQuery : IRequest<ApiResponse<bool>>
    {
        public DeleteDesignationRequestDTO DTO { get; }

        /// <summary>
        /// Initializes the delete request.
        /// </summary>
        public DeleteDesignationQuery(DeleteDesignationRequestDTO dto)
        {
            DTO = dto;
        }
    }

    #endregion

    #region Handler

    /// <summary>
    /// Handles soft-deletion requests for designations owned by the authenticated institution.
    /// </summary>
    public class DeleteDesignationQueryHandler : IRequestHandler<DeleteDesignationQuery, ApiResponse<bool>>
    {
        #region Fields

        private readonly IUnitOfWork _unitOfWork;
        private readonly ICommonRequestService _commonRequestService;
        private readonly ILogger<DeleteDesignationQueryHandler> _logger;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes the handler dependencies.
        /// </summary>
        public DeleteDesignationQueryHandler(
            IUnitOfWork unitOfWork,
            ICommonRequestService commonRequestService,
            ILogger<DeleteDesignationQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _commonRequestService = commonRequestService;
            _logger = logger;
        }

        #endregion

        #region Handle

        /// <summary>
        /// Soft deletes a designation using trusted institution and actor identifiers.
        /// </summary>
        public async Task<ApiResponse<bool>> Handle(
            DeleteDesignationQuery request,
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
                    "Invalid Institution authorization context while deleting Designation. InstitutionId: {InstitutionId}, StaffMemberId: {StaffMemberId}, TokenRoleId: {TokenRoleId}",
                    institutionId,
                    userStaffMemberId,
                    tokenRoleId);

                throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
            }

            #endregion

            if (request.DTO.Id <= 0)
                throw new ValidationErrorException(
                    AppConstants.ErrorMessages.InvalidIdentifier);

            // An inactive staffMember can be reactivated, so only a soft-deleted StaffMember
            // is safe to ignore while validating Designation deletion dependencies.
            var hasStaffMembers = await _unitOfWork.DesignationRepository
                .HasNonDeletedStaffMembersAsync(
                    request.DTO.Id,
                    institutionId,
                    cancellationToken);

            if (hasStaffMembers)
            {
                _logger.LogWarning(
                    "Designation deletion blocked because non-soft-deleted staffMembers are assigned. DesignationId: {DesignationId}, InstitutionId: {InstitutionId}",
                    request.DTO.Id,
                    institutionId);

                throw new ConflictException(
                    AppConstants.ErrorMessages.DesignationHasStaffMembers,
                    AppConstants.ErrorCodes.DesignationHasStaffMemberDependencies);
            }

            var deleted = await _unitOfWork.DesignationRepository.DeleteDesignationAsync(
                request.DTO.Id,
                institutionId,
                userStaffMemberId,
                cancellationToken);

            if (!deleted)
                throw new NotFoundException(
                    AppConstants.ErrorMessages.ResourceNotFound);

            _logger.LogInformation("Designation deleted. DesignationId: {DesignationId}", request.DTO.Id);
            return ApiResponse<bool>.Success(true, "Designation deleted successfully.");
        }

        #endregion
    }

    #endregion
}
