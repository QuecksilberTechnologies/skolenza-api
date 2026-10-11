// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles Get StaffMember Summary Query Handler requests.
// ================================================================

using skolenza.application.Common.Helpers.ProjectionHelpers.Staff;
using skolenza.application.Common.Helpers.RequestHelper;
using skolenza.application.Constants;
using skolenza.application.DTOS.Staff.BaseStaffMember;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.IFileStorage;
using skolenza.application.Interfaces.IPermission;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.StaffMemberCmd.StaffMemberBase.Handlers
{

    #region Query

    /// <summary>
    /// Represents the GetStaffMemberSummaryQuery application component.
    /// </summary>
    public class GetStaffMemberSummaryQuery : IRequest<ApiResponse<SummaryStaffMemberInfo>>
    {
        public GetStaffMemberSummaryRequestDTO DTO { get; }

        public GetStaffMemberSummaryQuery(GetStaffMemberSummaryRequestDTO dTO)
        {
            DTO = dTO;
        }

    }
    /// <summary>
    /// Handles GetStaffMemberSummaryQuery requests.
    /// </summary>
        #endregion

    #region Handler

public class GetStaffMemberSummaryQueryHandler
       : IRequestHandler<GetStaffMemberSummaryQuery, ApiResponse<SummaryStaffMemberInfo>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetStaffMemberSummaryQueryHandler> _logger;
        private readonly IPermissionService _permissionService;
        private readonly ICommonRequestService _commonRequestService;
        private readonly IConfiguration _config;
        private readonly IIdEncoderService _idEncoderService;
        private readonly IFileStorageService _fileStorageService;



        public GetStaffMemberSummaryQueryHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetStaffMemberSummaryQueryHandler> logger,
            IPermissionService permissionService,
            ICommonRequestService commonRequestService,
            IIdEncoderService idEncoderService,
            IConfiguration config, IFileStorageService fileStorageService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _permissionService = permissionService;
            _commonRequestService = commonRequestService;
            _idEncoderService = idEncoderService;
            _config = config;
            _fileStorageService = fileStorageService;
        }

        public async Task<ApiResponse<SummaryStaffMemberInfo>> Handle(
    GetStaffMemberSummaryQuery request,
    CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("GetStaffMemberSummary started");

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

                if (!await _commonRequestService.CanAccessStaffMemberDataAsync(
                        validation,
                        staffMemberId,
                        StaffMemberDataAccessRequirement.PersonalDetails,
                        cancellationToken))
                    throw new ForbiddenAccessException(AppConstants.ErrorMessages.PermissionDenied);
                // 5️⃣ FETCH SUMMARY
                // ===============================
                var summary =
                    await _unitOfWork.StaffMembers
                        .BuildStaffMemberSummaryAsync(
                            staffMemberId,
                            request.DTO.IsActive);

                if (summary == null)
                {
                    _logger.LogInformation(
                        "No StaffMember Summary found | StaffMemberId: {StaffMemberId}",
                        staffMemberId);

                    throw new ApiException("StaffMember summary not found.", 404);
                }

                // ===============================
                // 6️⃣ PROJECTION
                // ===============================
                var result =
                    ProjectionHelper.ToGetSummaryResponseDTO(
                        summary,
                        _idEncoderService,
                        validation.Claims.InstitutionEncriptionKey,
                        _config, _fileStorageService);

                _logger.LogInformation("GetStaffMemberSummary success");

                // ===============================
                // 7️⃣ SUCCESS
                // ===============================
                return ApiResponse<SummaryStaffMemberInfo>
                    .Success(
                        result,
                        "StaffMember summary retrieved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error fetching staffMember summary | StaffMemberId: {StaffMemberId}",
                    request.DTO?.StaffMemberId);

                throw; //  MUST
            }
        }
    }

    #endregion
}
