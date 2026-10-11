// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Handles GetContactInfoQueryHandler requests using authenticated institution context.
// ================================================================

using skolenza.application.Common.Helpers.ProjectionHelpers.Staff;
using skolenza.application.Common.Helpers.RequestHelper;
using skolenza.application.DTOS.Staff.Contact;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.IPermission;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;
using Microsoft.Extensions.Logging;


namespace skolenza.application.Features.StaffMemberCmd.Contact.Handlers
{

    #region Query

    /// <summary>
    /// Represents the GetContactInfoQuery application component.
    /// </summary>
    public class GetContactInfoQuery : IRequest<ApiResponse<List<GetContactResponseDTO>>>
    {
        public GetContactRequestDTO DTO { get; set; }

        public GetContactInfoQuery(GetContactRequestDTO dto)
        {
            DTO = dto;
        }

    }
    /// <summary>
    /// Handles authenticated institution requests for this feature.
    /// </summary>
        #endregion

    #region Handler

public class GetContactInfoQueryHandler
    : IRequestHandler<GetContactInfoQuery, ApiResponse<List<GetContactResponseDTO>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetContactInfoQueryHandler> _logger;
        private readonly IPermissionService _permissionService;
        private readonly IIdEncoderService _idEncoderService;
        private readonly ICommonRequestService _commonRequestService;

        public GetContactInfoQueryHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetContactInfoQueryHandler> logger,
            IPermissionService permissionService,
            IIdEncoderService idEncoderService,
            ICommonRequestService commonRequestService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _permissionService = permissionService;
            _idEncoderService = idEncoderService;
            _commonRequestService = commonRequestService;
        }


        public async Task<ApiResponse<List<GetContactResponseDTO>>> Handle(
        GetContactInfoQuery request,
        CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("GetContactInfo started");

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
                // 4️⃣ FETCH DATA
                // ===============================
                var result =
                    await _unitOfWork.StaffMemberContactRepository
                        .GetInfo(staffMemberId, request.DTO);

                // ===============================
                // 5️⃣ OPTIMIZED EMPTY HANDLING
                // ===============================
                var items = result?.Data ?? new List<GetContactResponseDTO>();

                var responseDTO = items.Any()
                    ? items // already DTO hai → projection ki zarurat nahi
                    : new List<GetContactResponseDTO>();

                _logger.LogInformation("GetContactInfo success");

                // ===============================
                // 6️⃣ SINGLE RESPONSE
                // ===============================
                return ApiResponse<List<GetContactResponseDTO>>
                    .SuccessPaginatedPercentage(
                        Data: responseDTO,
                        Message: items.Any()
                            ? "Contact info retrieved successfully."
                            : "No contact info found.",
                        PageNumber: result?.PageNumber ?? 1,
                        PageSize: result?.PageSize ?? 0,
                        TotalRecords: result?.TotalCount ?? 0,
                        TotalPages: result?.TotalPages ?? 0,
                        CompletionPercentage: result?.CompletionPercentage ?? 0,
                        HasUploadedAll: result?.HasUploadedAll ?? false
                    );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error fetching contact info | StaffMemberId: {StaffMemberId}",
                    request.DTO?.StaffMemberId);

                throw; //  MUST
            }
        }
    }


    #endregion
}
