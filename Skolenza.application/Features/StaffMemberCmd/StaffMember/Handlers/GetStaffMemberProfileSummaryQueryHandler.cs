// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles Get StaffMember Profile Summary Query Handler requests.
// ================================================================

using skolenza.application.Common.Helpers.ProjectionHelpers.Staff;
using skolenza.application.Common.Helpers.RequestHelper;
using skolenza.application.Constants;
using skolenza.application.DTOS.Staff.BaseStaffMember;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.StaffMemberCmd.StaffMemberBase.Handlers
{

    #region Query

    // =====================================================
    // QUERY
    // =====================================================
    /// <summary>
    /// Represents the GetStaffMemberProfileSummaryQuery application component.
    /// </summary>
    public class GetStaffMemberProfileSummaryQuery
        : IRequest<ApiResponse<StaffMemberProfileSummaryInfo>>
    {
        public GetStaffMemberSummaryRequestDTO DTO { get; }

        public GetStaffMemberProfileSummaryQuery(GetStaffMemberSummaryRequestDTO dto)
        {
            DTO = dto;
        }
    }

    // =====================================================
    // HANDLER
    // =====================================================
    /// <summary>
    /// Handles GetStaffMemberProfileSummaryQuery requests.
    /// </summary>
        #endregion

    #region Handler

public class GetStaffMemberProfileSummaryQueryHandler
        : IRequestHandler<GetStaffMemberProfileSummaryQuery, ApiResponse<StaffMemberProfileSummaryInfo>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetStaffMemberProfileSummaryQueryHandler> _logger;
        private readonly ICommonRequestService _commonRequestService;
        private readonly IIdEncoderService _idEncoderService;

        public GetStaffMemberProfileSummaryQueryHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetStaffMemberProfileSummaryQueryHandler> logger,
            ICommonRequestService commonRequestService,
            IIdEncoderService idEncoderService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _commonRequestService = commonRequestService;
            _idEncoderService = idEncoderService;
        }

        public async Task<ApiResponse<StaffMemberProfileSummaryInfo>> Handle(
      GetStaffMemberProfileSummaryQuery request,
      CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("GetStaffMemberProfileSummary started");

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

                // ===============================
                // 3️⃣ DECODE STAFF_MEMBER ID
                // ===============================
                var staffMemberId =
                    RequestCommonHelper.DecodeOnlyStaffMemberId(
                        request.DTO.StaffMemberId,
                        validation.Claims.InstitutionEncriptionKey,
                        _idEncoderService);

                if (staffMemberId <= 0)
                    throw new ValidationErrorException("Invalid StaffMemberId.");

                var canAccessTarget = await _commonRequestService.CanAccessStaffMemberDataAsync(
                    validation,
                    staffMemberId,
                    StaffMemberDataAccessRequirement.DirectoryBasic,
                    cancellationToken);
                if (!canAccessTarget)
                {
                    throw new ForbiddenAccessException(AppConstants.ErrorMessages.PermissionDenied);
                }
                // 5️⃣ FETCH SUMMARY
                // ===============================
                var summary =
                    await _unitOfWork.StaffMembers.StaffMemberProfileSummaryAsync(
                        staffMemberId,
                        request.DTO.IsActive);

                if (summary == null)
                {
                    _logger.LogInformation(
                        "No StaffMember Profile Summary found | StaffMemberId: {StaffMemberId}",
                        staffMemberId);

                    throw new ApiException("StaffMember profile summary not found.", 404);
                }

                // ===============================
                // 6️⃣ PROJECTION
                // ===============================
                var response =
                    ProjectionHelper.ToGetProfileSummaryResponseDTO(
                        summary,
                        _idEncoderService,
                        validation.Claims.InstitutionEncriptionKey);

                // StaffMemberInstitutionPermissionBehavior has already completed the Module and
                // stored-procedure permission check. Response visibility derives only from
                // the validated token context and decoded target StaffMemberId.
                response = ProjectionHelper.ApplyStaffMemberProfileSummaryVisibility(
                    response,
                    validation.LoggedInStaffMemberId,
                    staffMemberId,
                    validation.RoleTypeId);

                _logger.LogInformation("GetStaffMemberProfileSummary success");

                // ===============================
                // 7️⃣ SUCCESS
                // ===============================
                return ApiResponse<StaffMemberProfileSummaryInfo>
                    .Success(
                        response,
                        "StaffMember profile summary retrieved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error fetching staffMember profile summary | StaffMemberId: {StaffMemberId}",
                    request.DTO?.StaffMemberId);

                throw; //  MUST
            }
        }
    }
    #endregion
}
