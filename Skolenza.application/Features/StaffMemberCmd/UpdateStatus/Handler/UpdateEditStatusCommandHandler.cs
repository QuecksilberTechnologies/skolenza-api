// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles Update Edit Status Command Handler requests.
// ================================================================

using AutoMapper;
using skolenza.application.Common.Enums;
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

namespace skolenza.application.Features.StaffMemberCmd.UpdateStatus.Handler
{

    #region Command



    /// <summary>
    /// Represents the UpdateEditableStatusCommand application component.
    /// </summary>
    public class UpdateEditableStatusCommand
       : IRequest<ApiResponse<bool>>
    {
        public UpdateEditStatusRequestDTO_ DTO { get; set; }

        public UpdateEditableStatusCommand(UpdateEditStatusRequestDTO_ dto)
        {
            DTO = dto;
        }
    }

    /// <summary>
    /// Handles UpdateEditStatusCommand requests.
    /// </summary>
        #endregion

    #region Handler

public class UpdateEditStatusCommandHandler
        : IRequestHandler<UpdateEditableStatusCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<UpdateEditStatusCommandHandler> _logger;
        private readonly ITokenService _tokenService;
        private readonly IPermissionService _permissionService;
        private readonly IConfiguration _config;
        private readonly IEncryptionService _encryptionService;
        private readonly IIdEncoderService _idEncoderService;
        private readonly ICommonRequestService _commonRequestService;

        public UpdateEditStatusCommandHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor,
            ILogger<UpdateEditStatusCommandHandler> logger,
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
                _logger.LogInformation("UpdateEditableStatus started");

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
                        .UpdateEditableStatusByEntityAsync(
                            request.DTO.TabInfoType,
                            staffMemberId,
                            validation.LoggedInStaffMemberId,
                            request.DTO.IsEditable,
                            ct);

                if (!updated)
                {
                    _logger.LogWarning(
                        "Editable update failed | EmpId={EmpId} | Tab={Tab}",
                        staffMemberId,
                        request.DTO.TabInfoType);

                    throw new ApiException("Editable update failed.", 500);
                }

                _logger.LogInformation("UpdateEditableStatus success");

                return ApiResponse<bool>.Success(
                    true,
                    "Editable update completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Editable update error");

                throw; //  MUST
            }
        }

    }
    #endregion
}
