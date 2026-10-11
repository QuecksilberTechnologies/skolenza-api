// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Handles GetBaseStaffMemberInfoQueryHandler requests using authenticated institution context.
// ================================================================

using AutoMapper;
using skolenza.application.Common.Helpers.ProjectionHelpers.Staff;
using skolenza.application.Common.Helpers.RequestHelper;
using skolenza.application.Constants;
using skolenza.application.DTOS.Staff.BaseStaffMember;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.IPermission;
using skolenza.application.Interfaces.ITokenService;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.StaffMemberCmd.StaffMemberBase.Handlers
{

    #region Query

    /// <summary>
    /// Represents the GetBaseStaffMemberInfoQuery application component.
    /// </summary>
    public class GetBaseStaffMemberInfoQuery : IRequest<ApiResponse<List<GetBaseStaffMemberResponseDTO>>>
    {
        public GetBaseStaffMemberRequestDTO DTO { get; }

        public GetBaseStaffMemberInfoQuery(GetBaseStaffMemberRequestDTO dTO)
        {
            DTO = dTO;
        }
    }
    /// <summary>
    /// Handles authenticated institution requests for this feature.
    /// </summary>
        #endregion

    #region Handler

public class GetBaseStaffMemberInfoQueryHandler : IRequestHandler<GetBaseStaffMemberInfoQuery, ApiResponse<List<GetBaseStaffMemberResponseDTO>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<GetBaseStaffMemberInfoQueryHandler> _logger;
        private readonly ITokenService _tokenService;
        private readonly IPermissionService _permissionService;
        private readonly IConfiguration _config;
        private readonly IEncryptionService _encryptionService;
        private readonly IIdEncoderService _idEncoderService;
        private readonly ICommonRequestService _commonRequestService;

        public GetBaseStaffMemberInfoQueryHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor,
            ILogger<GetBaseStaffMemberInfoQueryHandler> logger,
            ITokenService tokenService,
            IPermissionService permissionService,
            IConfiguration config,
            IEncryptionService encryptionService, IIdEncoderService idEncoderService, ICommonRequestService commonRequestService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _tokenService = tokenService;
            _permissionService = permissionService;
            _config = config;
            _encryptionService = encryptionService;
            _idEncoderService = idEncoderService;
            _commonRequestService = commonRequestService;
        }

        public async Task<ApiResponse<List<GetBaseStaffMemberResponseDTO>>> Handle(
     GetBaseStaffMemberInfoQuery request,
     CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("GetBaseStaffMemberInfo started");

                // ===============================
                // 1️⃣ VALIDATION
                // ===============================
                #region Institution Request Validation
                var validation =
                    await _commonRequestService.ValidateInstitutionUserRequestAsync();
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

                if (!await _commonRequestService.CanAccessStaffMemberDataAsync(
                        validation,
                        staffMemberId,
                        StaffMemberDataAccessRequirement.PersonalDetails,
                        cancellationToken))
                    throw new ForbiddenAccessException(AppConstants.ErrorMessages.PermissionDenied);
                // 4️⃣ FETCH DATA
                // ===============================
                var responseDTO =
                    await _unitOfWork.StaffMembers.GetInfo(
                        validation.InstitutionId,
                        staffMemberId,
                        request.DTO);

                // ===============================
                // 5️⃣ OPTIMIZED EMPTY HANDLING
                // ===============================
             var items = responseDTO?.Data ?? new List<GetBaseStaffMemberResponseDTO>();

                var resultList = items.Any()
                    ? ProjectionHelper.ToGetBaseInfoListResponseDTOs(
                        items,
                        _idEncoderService,
                        validation.Claims.InstitutionEncriptionKey)
                    : new List<GetBaseStaffMemberResponseDTO>();

                _logger.LogInformation("GetBaseStaffMemberInfo success");

                // ===============================
                // 6️⃣ SAFE COMPLETION %
                // ===============================
                var completion =
                    resultList.Any()
                        ? Math.Round(resultList.Average(r => r.CompletionPercentage), 0)
                        : 0;

                // ===============================
                // 7️⃣ SINGLE RESPONSE
                // ===============================
                return ApiResponse<List<GetBaseStaffMemberResponseDTO>>
                    .SuccessPaginatedPercentage(
                        Data: resultList,
                        Message: resultList.Any()
                            ? "StaffMember info retrieved successfully."
                            : "No staffMember info found.",
                        PageNumber: responseDTO?.PageNumber ?? 1,
                        PageSize: responseDTO?.PageSize ?? 0,
                        TotalRecords: responseDTO?.TotalCount ?? 0,
                        TotalPages: responseDTO?.TotalPages ?? 0,
                        HasUploadedAll: responseDTO?.HasUploadedAll ?? false,
                        CompletionPercentage: completion
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
