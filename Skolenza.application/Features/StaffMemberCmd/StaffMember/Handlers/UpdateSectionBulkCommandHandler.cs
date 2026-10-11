// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles Update Section Bulk Command Handler requests.
// ================================================================

using skolenza.application.Common.Enums;
using skolenza.application.Constants;
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

    #region Command

    // ============================
    // COMMAND
    // ============================
    /// <summary>
    /// Represents the UpdateSectionBulkCommand application component.
    /// </summary>
    public class UpdateSectionBulkCommand
        : IRequest<ApiResponse<bool>>
    {
        public UpdateStaffMemberSectionStatusRequestDTO DTO { get; }

        public UpdateSectionBulkCommand(UpdateStaffMemberSectionStatusRequestDTO dto)
        {
            DTO = dto;
        }
    }

    // ============================
    // HANDLER
    // ============================
    /// <summary>
    /// Handles UpdateSectionBulkCommand requests.
    /// </summary>
        #endregion

    #region Handler

public class UpdateSectionBulkCommandHandler
        : IRequestHandler<UpdateSectionBulkCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UpdateSectionBulkCommandHandler> _logger;
        private readonly ICommonRequestService _commonRequestService;
        private readonly IPermissionService _permissionService;
        private readonly IIdEncoderService _idEncoderService;

        public UpdateSectionBulkCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<UpdateSectionBulkCommandHandler> logger,
            ICommonRequestService commonRequestService,
            IPermissionService permissionService,
            IIdEncoderService idEncoderService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _commonRequestService = commonRequestService;
            _permissionService = permissionService;
            _idEncoderService = idEncoderService;
        }

        public async Task<ApiResponse<bool>> Handle(
    UpdateSectionBulkCommand request,
    CancellationToken ct)
        {
            try
            {
                _logger.LogInformation("UpdateSectionBulk started");

                // ===============================
                // 1️⃣ VALIDATION
                // ===============================
                var validation =
                    await _commonRequestService.ValidateInstitutionUserRequestAsync();

                if (!validation.Success)
                    throw new UnauthorizedAccessException(validation.ErrorMessage);

                if (validation.RoleTypeId != ConstantValues.RoleTypeAdmin)
                    throw new ForbiddenAccessException(AppConstants.ErrorMessages.PermissionDenied);

                // ===============================
                // 2️⃣ NULL SAFETY
                // ===============================
                if (request?.DTO == null)
                    throw new ValidationErrorException("Invalid request.");

                // ===============================
                // 3️⃣ DECODE STAFF_MEMBER ID
                // ===============================
                var staffMemberId =
                    RequestCommonHelper.DecodeOnlyStaffMemberId(
                        request.DTO.StaffMemberId,
                        validation.Claims.InstitutionEncriptionKey,
                        _idEncoderService);

                if (staffMemberId <= 0)
                    throw new ValidationErrorException("Invalid staffMember.");

                // ===============================
                // 4️⃣ VALIDATE SECTIONS
                // ===============================
                if (request.DTO.Sections == null || !request.DTO.Sections.Any())
                    throw new ValidationErrorException("No section selected.");

                if (request.DTO.Sections.Any(section =>
                        !Enum.IsDefined(typeof(TabInfoType), section.TabInfoType)))
                    throw new ValidationErrorException("Unsupported verification section.");

                if (request.DTO.Sections.Select(section => section.TabInfoType).Distinct().Count() != request.DTO.Sections.Count)
                    throw new ValidationErrorException("A section may only appear once.");

                await _unitOfWork.BeginTransactionAsync();
                // 6️⃣ LOOP (OPTIMIZED)
                // ===============================
                foreach (var section in request.DTO.Sections)
                {
                    if (!Enum.IsDefined(typeof(TabInfoType), section.TabInfoType))
                    {
                        _logger.LogWarning(
                            "Invalid TabInfoType | Value={Value} | EmpId={EmpId}",
                            section.TabInfoType,
                            staffMemberId
                        );
                        continue;
                    }

                    var updated =
                        await _unitOfWork.StaffMembers
                            .UpdateSectionVerifyStatusAsync(
                                (int)section.TabInfoType,
                                staffMemberId,
                                validation.InstitutionId,
                                section.IsVerified ?? false,
                                section.IsEditAllowed ?? false,
                                validation.UserStaffMemberId,
                                ct
                            );

                    if (!updated)
                    {
                        _logger.LogWarning(
                            "Section update failed | Section={Section} | EmpId={EmpId}",
                            section.TabInfoType,
                            staffMemberId
                        );
                        throw new ValidationErrorException("A selected section has no editable records or does not belong to this institution.");
                    }
                }

                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("UpdateSectionBulk success");

                // ===============================
                // 7️⃣ SUCCESS
                // ===============================
                return ApiResponse<bool>.Success(
                    true,
                    "Section status updated successfully.");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "UpdateSectionBulk failed");

                throw; //  MUST
            }
        }


    }

    #endregion
}
