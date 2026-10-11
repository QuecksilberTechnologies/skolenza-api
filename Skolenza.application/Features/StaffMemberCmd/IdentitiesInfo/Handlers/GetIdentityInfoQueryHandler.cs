using skolenza.application.Common.Helpers.ProjectionHelpers.Staff;
using skolenza.application.Common.Helpers.RequestHelper;
using skolenza.application.DTOS.Staff.Sensitive;
using skolenza.application.DTOS.StoreProcedures;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.IPermission;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.StaffMemberCmd.IdentitiesInfo.Handlers
{
    #region Query Definition
    public class GetIdentityInfoQuery
        : IRequest<ApiResponse<List<GetStaffMemberIdentityResponseDTO>>>
    {
        public GetIdentityRequestDTO DTO { get; }

        public GetIdentityInfoQuery(GetIdentityRequestDTO dto)
        {
            DTO = dto ?? throw new ArgumentNullException(nameof(dto));
        }
    }

    #endregion

    #region Query Handler

    public class GetIdentityInfoQueryHandler
 : IRequestHandler<GetIdentityInfoQuery, ApiResponse<List<GetStaffMemberIdentityResponseDTO>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetIdentityInfoQueryHandler> _logger;
        private readonly IPermissionService _permissionService;
        private readonly ICommonRequestService _commonRequestService;
        private readonly IIdEncoderService _idEncoderService;

        public GetIdentityInfoQueryHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetIdentityInfoQueryHandler> logger,
            IPermissionService permissionService,
            ICommonRequestService commonRequestService,
            IIdEncoderService idEncoderService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _permissionService = permissionService;
            _commonRequestService = commonRequestService;
            _idEncoderService = idEncoderService;
        }

        public async Task<ApiResponse<List<GetStaffMemberIdentityResponseDTO>>> Handle(
   GetIdentityInfoQuery request,
   CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("GetIdentityInfo started");

                // ===============================
                // 1️⃣ VALIDATION
                // ===============================
                var validation =
                    await _commonRequestService
                        .ValidateInstitutionUserRequestAsync();

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
                var staffMemberId = string.IsNullOrWhiteSpace(request.DTO.StaffMemberId)
                    ? validation.LoggedInStaffMemberId
                    : RequestCommonHelper.DecodeOnlyStaffMemberId(
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
                {
                    throw new ForbiddenAccessException("StaffMember identity access denied.");
                }

                var staffMember = await _unitOfWork.StaffMembers.GetByIdAsync(
                    staffMemberId,
                    validation.InstitutionId,
                    track: false)
                    ?? throw new NotFoundException("StaffMember not found.");
                // 5️⃣ FETCH DATA
                // ===============================
                var spRecords =
                    await _unitOfWork.StoreProcedureRepository
                        .GetIdentityRecordAsync(
                            staffMemberId,
                            staffMember.CountryId,
                            true);

                // ===============================
                // 6️⃣ SAFE EMPTY HANDLING
                // ===============================
                var items = spRecords ?? new List<GetStaffMemberIdentitySp>(); // 🔁 replace with actual type

                var response = items.Any()
                    ? ProjectionHelper.ToGetIdentityResponseDTO(
                        items,
                        _idEncoderService,
                        validation.Claims.InstitutionEncriptionKey)
                    : new List<GetStaffMemberIdentityResponseDTO>();

                _logger.LogInformation("GetIdentityInfo success");

                // ===============================
                // 7️⃣ SUCCESS
                // ===============================
                var apiResponse = ApiResponse<List<GetStaffMemberIdentityResponseDTO>>
                    .Success(
                        response,
                        response.Any()
                            ? "Identity information retrieved successfully."
                            : "Identity information not found."
                    );

                apiResponse.CompletionPercentage = response.Count == 0
                    ? 0
                    : Math.Round(response.Average(item => item.CompletionPercentage), 0);

                return apiResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching identity info");

                throw; // 🚨 MUST
            }
        }
    }


    #endregion
}
