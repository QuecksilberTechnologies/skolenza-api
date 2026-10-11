// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Handles GetBankInfoQueryHandler requests using authenticated institution context.
// ================================================================

using AutoMapper;
using skolenza.application.Common.Helpers.ProjectionHelpers.Staff;
using skolenza.application.Common.Helpers.RequestHelper;
using skolenza.application.DTOS.Staff.Bank;
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

namespace skolenza.application.Features.StaffMemberCmd.BankInfo.Handlers
{

    #region Query

    /// <summary>
    /// Represents the GetBankInfoQuery application component.
    /// </summary>
    public class GetBankInfoQuery : IRequest<ApiResponse<List<GetBankResponseDTO>>>
    {
        public GetBankReqestDTO DTO { get; set; }

        public GetBankInfoQuery(GetBankReqestDTO dTO)
        {
            DTO = dTO;
        }
    }
    /// <summary>
    /// Handles authenticated institution requests for this feature.
    /// </summary>
        #endregion

    #region Handler

public class GetBankInfoQueryHandler : IRequestHandler<GetBankInfoQuery, ApiResponse<List<GetBankResponseDTO>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<GetBankInfoQueryHandler> _logger;
        private readonly ITokenService _tokenService;
        private readonly IPermissionService _permissionService;
        private readonly IConfiguration _config;
        private readonly IEncryptionService _encryptionService;
        private readonly IIdEncoderService _idEncoderService;
        private readonly ICommonRequestService _commonRequestService;
        private readonly IFileStorageService _fileStorageService ;


        public GetBankInfoQueryHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor,
            ILogger<GetBankInfoQueryHandler> logger,
            ITokenService tokenService,
            IPermissionService permissionService,
            IConfiguration config,
             IEncryptionService encryptionService, IIdEncoderService idEncoderService, ICommonRequestService commonRequestService,
             IFileStorageService fileStorageService)
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
            _fileStorageService = fileStorageService;
        }

        public async Task<ApiResponse<List<GetBankResponseDTO>>> Handle(
      GetBankInfoQuery request,
      CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("GetBankInfo started");

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
                // 4️⃣ FETCH DATA
                // ===============================
                var bankEntities =
                    await _unitOfWork.StaffMemberBankRepository
                        .GetInfoAsync(staffMemberId, request.DTO);




                // ===============================
                // 5️⃣ EMPTY LIST = SUCCESS
                // ===============================
                if (bankEntities == null || bankEntities.Data == null || !bankEntities.Data.Any())
                {
                    return ApiResponse<List<GetBankResponseDTO>>
                        .SuccessPaginatedPercentage(
                            Data: new List<GetBankResponseDTO>(),
                            Message: "No bank info found.",
                            PageNumber: bankEntities?.PageNumber ?? 1,
                            PageSize: bankEntities?.PageSize ?? 0,
                            TotalRecords: bankEntities?.TotalCount ?? 0,
                            TotalPages: bankEntities?.TotalPages ?? 0,
                            HasUploadedAll: bankEntities?.HasUploadedAll ?? false,
                            CompletionPercentage: bankEntities?.CompletionPercentage ?? 0
                        );
                }

                // ===============================
                // 6️⃣ PROJECTION
                // ===============================
                var result = ProjectionHelper.ToGetBankResponseDTOs(
                    bankEntities,
                    _idEncoderService,
                    _encryptionService,
                    validation.Claims.InstitutionEncriptionKey,
                    _config, _fileStorageService
                );

                // ===============================
                // 7️⃣ SUCCESS RESPONSE
                // ===============================
                _logger.LogInformation("GetBankInfo success");

                return ApiResponse<List<GetBankResponseDTO>>
                    .SuccessPaginatedPercentage(
                        Data: result,
                        Message: "Bank info retrieved successfully.",
                        PageNumber: bankEntities.PageNumber,
                        PageSize: bankEntities.PageSize,
                        TotalRecords: bankEntities.TotalCount,
                        TotalPages: bankEntities.TotalPages,
                        HasUploadedAll: bankEntities.HasUploadedAll,
                        CompletionPercentage: bankEntities.CompletionPercentage
                    );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error fetching bank info | StaffMemberId: {StaffMemberId}",
                    request.DTO?.StaffMemberId);

                throw; //  MUST
            }
        }
    }


    #endregion
}

