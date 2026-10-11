// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Handles GetAllStaffMemberInfoQueryHandler requests using authenticated institution context.
// ================================================================



using skolenza.application.Constants;
using skolenza.application.Common.Helpers.ProjectionHelpers.Staff;
using skolenza.application.Common.Helpers.RequestHelper;
using skolenza.application.DTOS.Staff.BaseStaffMember;
using skolenza.application.DTOS.Pagination;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.IFileStorage;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.StaffMemberCmd.StaffMemberBase.Handlers
{

    #region Query

    /// <summary>
    /// Represents the GetAllStaffMemberInfoQuery application component.
    /// </summary>
    public class GetAllStaffMemberInfoQuery : IRequest<ApiResponse<List<GetAllStaffMemberInfoResponseDTO>>>
    {
        public GetAllStaffMemberInfoRequestDTO DTO { get; }

        public GetAllStaffMemberInfoQuery(GetAllStaffMemberInfoRequestDTO dTO)
        {
            DTO = dTO;
        }
    }
    /// <summary>
    /// Handles authenticated institution requests for this feature.
    /// </summary>
        #endregion

    #region Handler

public class GetAllStaffMemberInfoQueryHandler : IRequestHandler<GetAllStaffMemberInfoQuery, ApiResponse<List<GetAllStaffMemberInfoResponseDTO>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetAllStaffMemberInfoQueryHandler> _logger;
        private readonly IIdEncoderService _idEncoderService;
        private readonly ICommonRequestService _commonRequestService;
        private readonly IFileStorageService _fileStorageService;

        public GetAllStaffMemberInfoQueryHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetAllStaffMemberInfoQueryHandler> logger,
            IIdEncoderService idEncoderService,
            ICommonRequestService commonRequestService,
            IFileStorageService fileStorageService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _idEncoderService = idEncoderService;
            _commonRequestService = commonRequestService;
            _fileStorageService = fileStorageService;
        }
        public async Task<ApiResponse<List<GetAllStaffMemberInfoResponseDTO>>> Handle(
    GetAllStaffMemberInfoQuery request,
    CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("GetAllStaffMemberInfo started");

                // ===============================
                // Validate the request.
                // ===============================
                #region Institution Request Validation
                var validation =
                    await _commonRequestService.ValidateInstitutionUserRequestAsync();
                #endregion

                if (!validation.Success)
                    throw new UnauthorizedAccessException(validation.ErrorMessage);

                // ===============================
                // Enforce required request data.
                // ===============================
                if (!validation.Success)
                    throw new UnauthorizedAccessException(validation.ErrorMessage);

                if (request?.DTO == null)
                    throw new ValidationErrorException("Invalid request");

                long? staffMemberId = null;
                if (!string.IsNullOrWhiteSpace(request.DTO.StaffMemberId))
                {
                    staffMemberId = RequestCommonHelper.DecodeOnlyStaffMemberId(
                        request.DTO.StaffMemberId,
                        validation.Claims.InstitutionEncriptionKey,
                        _idEncoderService);

                    if (staffMemberId <= 0)
                        throw new ValidationErrorException("Invalid StaffMemberId.");
                }
                // Retrieve the requested staffMember records.
                // ===============================
                var responseDTO = await _unitOfWork.StaffMembers.GetAllInfo(
                    validation.InstitutionId,
                    staffMemberId,
                    request.DTO,
                    validation.LoggedInStaffMemberId,
                    validation.RoleTypeId);

                if (responseDTO == null)
                    throw new ApiException("StaffMember data not found", 404);

                // ===============================
                // Preserve the existing empty-result behavior.
                // ===============================
                var items = responseDTO?.Data ?? new List<GetAllStaffMemberInfoResponseDTO>();

                // StaffMemberInstitutionPermissionBehavior has already enforced the Module and stored-procedure
                // permission check. The validated context below supplies the trusted viewer identity used
                // only to shape the response; no client-provided role or StaffMemberId controls visibility.
                var resultList = items.Any()
                    ? ProjectionHelper.ToGetAllStaffMemberInfoResponseDTOs(responseDTO, _idEncoderService,
                        validation.Claims.InstitutionEncriptionKey,
                        _fileStorageService,
                        validation.LoggedInStaffMemberId,
                        validation.RoleTypeId)
                    : new List<GetAllStaffMemberInfoResponseDTO>();

                // Expose the configured assignment limit on every existing StaffMember row without another role query.
                foreach (var staffMember in resultList)
                {
                    staffMember.MaxRoleAssigned = AppConstants.MaxStaffMemberRoleAssigned;
                }

                _logger.LogInformation("GetAllStaffMemberInfo success");

                // ===============================
                // Build the standardized response.
                // ===============================
                return ApiResponse<List<GetAllStaffMemberInfoResponseDTO>> .SuccessPaginatedPercentage(
                        Data: resultList,
                        Message: items.Any()
                            ? "StaffMember info retrieved successfully."
                            : "No staffMember info found.",
                        PageNumber: responseDTO?.PageNumber ?? 1,
                        PageSize: responseDTO?.PageSize ?? 0,
                        TotalRecords: responseDTO?.TotalCount ?? 0,
                        TotalPages: responseDTO?.TotalPages ?? 0,
                        CompletionPercentage: responseDTO?.CompletionPercentage ?? 0,
                        HasUploadedAll: responseDTO?.HasUploadedAll ?? false
                    );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error fetching staffMember info | StaffMemberId: {StaffMemberId}",
                    request.DTO?.StaffMemberId);

                throw; //  MUST
            }
        }

    }
    #endregion
}


