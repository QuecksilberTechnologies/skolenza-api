// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Handles BankEditableStatusCommandUpdateHandler requests using authenticated institution context.
// ================================================================

using AutoMapper;
using skolenza.application.Common.Helpers.RequestHelper;
using skolenza.application.DTOS.Common;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.IPermission;
using skolenza.application.Interfaces.ITokenService;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.StaffMemberCmd.BankInfo.Handlers
{

    #region Command

    /// <summary>
    /// Represents the UpdateEditableStatusCommand application component.
    /// </summary>
    public class UpdateEditableStatusCommand
      : IRequest<ApiResponse<bool>>
    {
        public UpdateEditStatusRequestDTO DTO { get; set; }

        public UpdateEditableStatusCommand(UpdateEditStatusRequestDTO dto)
        {
            DTO = dto;
        }
    }

    /// <summary>
    /// Handles authenticated institution requests for this feature.
    /// </summary>
        #endregion

    #region Handler

public class BankEditableStatusCommandUpdateHandler
        : IRequestHandler<UpdateEditableStatusCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<BankEditableStatusCommandUpdateHandler> _logger;
        private readonly ITokenService _tokenService;
        private readonly IPermissionService _permissionService;
        private readonly IConfiguration _config;
        private readonly IEncryptionService _encryptionService;
        private readonly IIdEncoderService _idEncoderService;
        private readonly ICommonRequestService _commonRequestService;

        public BankEditableStatusCommandUpdateHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor,
            ILogger<BankEditableStatusCommandUpdateHandler> logger,
            ITokenService tokenService,
            IPermissionService permissionService,
            IConfiguration config, ICommonRequestService commonRequestService,

            IEncryptionService encryptionService, IIdEncoderService idEncoderService)
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
        public async Task<ApiResponse<bool>> Handle(
    UpdateEditableStatusCommand request,
    CancellationToken ct)
        {
            try
            {
                _logger.LogInformation("Updating editable status");

                // ===============================
                // 1️⃣ COMMON VALIDATION (AUTH + CONTEXT)
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
                    throw new ValidationErrorException(
                        "Invalid request.",
                        new List<string> { "Request DTO is required." }
                    );

                // ===============================
                // 3️⃣ DECODE STAFF_MEMBER ID (IMPORTANT)
                // ===============================
                var staffMemberId = RequestCommonHelper.DecodeOnlyStaffMemberId(
                        request.DTO.StaffMemberId,
                        validation.Claims.InstitutionEncriptionKey,
                        _idEncoderService);

                if (staffMemberId <= 0)
                    throw new ValidationErrorException(
                        "Invalid StaffMember Id.",
                        new List<string> { "StaffMemberId is invalid after decoding." }
                    );

                // ===============================
                // 5️⃣ FETCH STAFF_MEMBER
                // ===============================
                var staffMember = await _unitOfWork.StaffMembers.GetByIdAsync(
                    staffMemberId,
                    validation.InstitutionId,
                    true);

                if (staffMember == null)
                {
                    _logger.LogWarning(
                        "StaffMember not found. StaffMemberId: {StaffMemberId}",
                        staffMemberId);

                    throw new ApiException("StaffMember not found.", 404);
                }

                // ===============================
                // 6️⃣ UPDATE EDITABLE STATUS
                // ===============================
                bool updateResult = await _unitOfWork.StaffMemberBankRepository
                    .UpdateEditStatus(
                        staffMemberId,
                        validation.LoggedInStaffMemberId,
                        request.DTO.IsEditable);

                if (!updateResult)
                {
                    _logger.LogWarning(
                        "Failed to update editable status for StaffMemberId: {StaffMemberId}",
                        staffMemberId);

                    throw new ApiException("Unexpected error occurred.", 500);
                }

                // ===============================
                // 7️⃣ SUCCESS RESPONSE
                // ===============================
                _logger.LogInformation(
                    "Editable status updated successfully for StaffMemberId: {StaffMemberId}",
                    staffMemberId);

                return ApiResponse<bool>
                    .Success(true, "Editable update completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Editable update error");

                //  IMPORTANT: middleware handle karega
                throw;
            }
        }


    }
    #endregion
}
