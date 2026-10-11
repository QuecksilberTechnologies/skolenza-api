// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles Update Verification Status Command Handler requests.
// ================================================================

using skolenza.application.Common.Enums;
using skolenza.application.Common.Helpers.RequestHelper;
using skolenza.application.DTOS.Common;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.IPermission;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.StaffMemberCmd.UpdateVerification.Handler
{

    #region Command

    // ============================
    // COMMAND
    // ============================

    /// <summary>
    /// Represents the UpdateVerificationStatusCommand application component.
    /// </summary>
    public class UpdateVerificationStatusCommand
        : IRequest<ApiResponse<bool>>
    {
        public UpdateVerificationStatusRequestDTO_ DTO { get; }

        public UpdateVerificationStatusCommand(UpdateVerificationStatusRequestDTO_ dto)
        {
            DTO = dto;
        }
    }

    // ============================
    // HANDLER
    // ============================
    /// <summary>
    /// Handles UpdateVerificationStatusCommand requests.
    /// </summary>
        #endregion

    #region Handler

public class UpdateVerificationStatusCommandHandler
        : IRequestHandler<UpdateVerificationStatusCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UpdateVerificationStatusCommandHandler> _logger;
        private readonly IIdEncoderService _idEncoderService;
        private readonly ICommonRequestService _commonRequestService;

        public UpdateVerificationStatusCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<UpdateVerificationStatusCommandHandler> logger,
            ICommonRequestService commonRequestService,
            IIdEncoderService idEncoderService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _commonRequestService = commonRequestService;
            _idEncoderService = idEncoderService;
        }

        public async Task<ApiResponse<bool>> Handle(
     UpdateVerificationStatusCommand request,
     CancellationToken ct)
        {
            try
            {
                _logger.LogInformation("UpdateVerificationStatus started");

                // ===============================
                // 1️⃣ VALIDATION
                // ===============================
                var validation =
                    await _commonRequestService.ValidateInstitutionUserRequestAsync();

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
                    throw new ValidationErrorException("Invalid staffMember.");

                // ===============================
                // 3️⃣ ENUM VALIDATION
                // ===============================
                if (!Enum.IsDefined(typeof(TabInfoType), request.DTO.TabInfoType))
                    throw new ValidationErrorException("Invalid section type.");
                // 5️⃣ FETCH STAFF_MEMBER
                // ===============================
                var staffMember =
                    await _unitOfWork.StaffMembers.GetByIdAsync(
                        staffMemberId,
                        validation.InstitutionId,
                        true);

                if (staffMember == null)
                    throw new ApiException("StaffMember not found.", 404);

                // ===============================
                // 6️⃣ UPDATE STATUS
                // ===============================
                var updated =
                    await _unitOfWork.StaffMembers
                        .UpdateVerificationStatusByTabAsync(
                            request.DTO.TabInfoType,
                            staffMemberId,
                            validation.LoggedInStaffMemberId,
                            request.DTO.IsVerified,
                            ct);

                if (!updated)
                {
                    _logger.LogWarning(
                        "Verification update failed | EmpId={EmpId} | Tab={Tab}",
                        staffMemberId,
                        request.DTO.TabInfoType);

                    throw new ApiException("Verification update failed.", 500);
                }

                _logger.LogInformation("UpdateVerificationStatus success");

                return ApiResponse<bool>.Success(
                    true,
                    "Verification status updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Verification update error");

                throw; //  MUST
            }
        }
    }

    #endregion
}

