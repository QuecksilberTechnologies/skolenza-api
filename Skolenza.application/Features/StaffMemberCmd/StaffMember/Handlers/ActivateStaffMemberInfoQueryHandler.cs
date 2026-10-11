// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Handles ActivateStaffMemberInfoQueryHandler requests using authenticated institution context.
// ================================================================

using skolenza.application.Common.Helpers.RequestHelper;
using skolenza.application.DTOS.Staff.BaseStaffMember;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.IPermission;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;


namespace skolenza.application.Features.StaffMemberCmd.StaffMemberBase.Handlers
{

    #region Query

    /// <summary>
    /// Represents the ActivateAllStaffMemberQuery application component.
    /// </summary>
    public class ActivateAllStaffMemberQuery : IRequest<ApiResponse<bool>>
    {
      public ActivateAllStaffMemberRequestDTO DTO;

        public ActivateAllStaffMemberQuery(ActivateAllStaffMemberRequestDTO dto)
        {
            DTO = dto;
        }
    }
    /// <summary>
    /// Handles authenticated institution requests for this feature.
    /// </summary>
        #endregion

    #region Handler

public class ActivateStaffMemberInfoQueryHandler
    : IRequestHandler<ActivateAllStaffMemberQuery, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ActivateStaffMemberInfoQueryHandler> _logger;
        private readonly ICommonRequestService _commonRequestService;
        private readonly IIdEncoderService _idEncoderService;
        private readonly IPermissionService _permissionService;

        public ActivateStaffMemberInfoQueryHandler(
            IUnitOfWork unitOfWork,
            ILogger<ActivateStaffMemberInfoQueryHandler> logger,
            ICommonRequestService commonRequestService,
            IIdEncoderService idEncoderService,
            IPermissionService permissionService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _commonRequestService = commonRequestService;
            _idEncoderService = idEncoderService;
            _permissionService = permissionService;
        }

        public async Task<ApiResponse<bool>> Handle(
    ActivateAllStaffMemberQuery request,
    CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("ActivateAllStaffMember started");

                // ===============================
                // 1️⃣ VALIDATION
                // ===============================
                #region Institution Request Validation
                var validation = await _commonRequestService
                    .ValidateInstitutionUserRequestAsync();
                #endregion

                if (!validation.Success)
                    throw new UnauthorizedAccessException(validation.ErrorMessage);

                // ===============================
                // 2️⃣ NULL SAFETY
                // ===============================
                if (request?.DTO == null)
                    throw new ValidationErrorException("Invalid request.");
                var staffMemberId = RequestCommonHelper.DecodeOnlyStaffMemberId(
                        request.DTO.StaffMemberId,
                        validation.Claims.InstitutionEncriptionKey,
                        _idEncoderService);

                if (staffMemberId <= 0)
                    throw new ValidationErrorException("Invalid StaffMemberId.");
                // 4️⃣ FETCH STAFF_MEMBER
                // ===============================
                var staffMember = await _unitOfWork.StaffMembers
                    .GetByIdAsync(
                        staffMemberId,
                        validation.InstitutionId,
                        true);

                if (staffMember == null)
                    throw new ApiException("StaffMember not found.", 404);

                // ===============================
                // 5️⃣ ACTIVATE / DEACTIVATE
                // ===============================
                var isSuccess = await _unitOfWork.StaffMembers
                    .ActivateAllStaffMemberAsync(staffMember, request.DTO.IsActive);

                if (!isSuccess)
                    throw new ApiException("Failed to update staffMember status.", 500);

                _logger.LogInformation("ActivateAllStaffMember success | Id: {Id}", staffMemberId);

                // ===============================
                // 6️⃣ SUCCESS RESPONSE
                // ===============================
                return ApiResponse<bool>.Success(
                    true,
                    request.DTO.IsActive
                        ? "StaffMember activated successfully."
                        : "StaffMember deactivated successfully."
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error updating staffMember status | Id: {Id}",
                    request.DTO?.StaffMemberId);

                throw; //  MUST
            }
        }
    }



    #endregion
}


