// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles Get Education Info Query Handler requests.
// ================================================================

using AutoMapper;
using skolenza.application.Common.Helpers.ProjectionHelpers.Staff;
using skolenza.application.Common.Helpers.RequestHelper;
using skolenza.application.DTOS.Staff.Education;
using skolenza.application.DTOS.Pagination;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.IFileStorage;
using skolenza.application.Interfaces.IPermission;
using skolenza.application.Interfaces.ITokenService;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.StaffMemberCmd.EducationInfo.Handlers
{

    #region Query

    /// <summary>
    /// Represents the GetEducationInfoQuery application component.
    /// </summary>
    public class GetEducationInfoQuery : IRequest<ApiResponse<List<GetEducationResponseDTO>>>
    {
        public GetEducationRequestDTO DTO { get; set; }

        public GetEducationInfoQuery(GetEducationRequestDTO dTO)
        {
            DTO = dTO;
        }
    }
    /// <summary>
    /// Handles GetEducationInfoQuery requests.
    /// </summary>
        #endregion

    #region Handler

public class GetEducationInfoQueryHandler : IRequestHandler<GetEducationInfoQuery, ApiResponse<List<GetEducationResponseDTO>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<GetEducationInfoQueryHandler> _logger;
        private readonly ITokenService _tokenService;
        private readonly IPermissionService _permissionService;
        private readonly IConfiguration _config;
        private readonly IEncryptionService _encryptionService;
        private readonly IIdEncoderService _idEncoderService;
        private readonly IConfiguration _configuration;
        private readonly ICommonRequestService _commonRequestService;
        private readonly IFileStorageService _fileStorageService;


        public GetEducationInfoQueryHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor,
            ILogger<GetEducationInfoQueryHandler> logger,
            ITokenService tokenService,
            IPermissionService permissionService,
            IConfiguration config,
          IEncryptionService encryptionService, IIdEncoderService idEncoderService, IConfiguration configuration, ICommonRequestService commonRequestService, IFileStorageService fileStorageService)
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
            this._configuration = configuration;
            _commonRequestService = commonRequestService;
            _fileStorageService = fileStorageService;
        }


        public async Task<ApiResponse<List<GetEducationResponseDTO>>> Handle(
    GetEducationInfoQuery request,
    CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("GetEducationInfo started");

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
                    throw new ValidationErrorException("Invalid StaffMemberId.");
                // 4️⃣ FETCH DATA
                // ===============================
                var entity =
                    await _unitOfWork.StaffMemberEducationRepository
                        .GetInfo(staffMemberId, request.DTO);

                // ===============================
                // 5️⃣ OPTIMIZED EMPTY HANDLING
                // ===============================
                var items = entity?.Data ?? new List<GetEducationResponseDTO>();

                var responseDTO = items.Any()
                    ? ProjectionHelper.ToGetEducationResponseDTOs(
                        entity,
                        _idEncoderService,
                        validation.Claims.InstitutionEncriptionKey,
                        _configuration, _fileStorageService)
                    : new List<GetEducationResponseDTO>();

                _logger.LogInformation("GetEducationInfo success");

                // ===============================
                // 6️⃣ SINGLE RESPONSE
                // ===============================
                return ApiResponse<List<GetEducationResponseDTO>>
                    .SuccessPaginatedPercentage(
                        Data: responseDTO,
                        Message: items.Any()
                            ? "Education info retrieved successfully."
                            : "No education info found.",
                        PageNumber: entity?.PageNumber ?? 1,
                        PageSize: entity?.PageSize ?? 0,
                        TotalRecords: entity?.TotalCount ?? 0,
                        TotalPages: entity?.TotalPages ?? 0,
                        CompletionPercentage: entity?.CompletionPercentage ?? 0,
                        HasUploadedAll: entity?.HasUploadedAll ?? false
                    );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error fetching education | StaffMemberId: {StaffMemberId}",
                    request.DTO?.UserStaffMemberId);

                throw; //  MUST
            }
        }

    }
    #endregion
}
