// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles Get Dependent Counts Query Handler requests.
// ================================================================

using skolenza.application.DTOS.Staff.Dependent;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.IFileStorage;
using skolenza.application.Interfaces.IPermission;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.StaffMemberCmd.DependentInfo.Handlers
{

    #region Query

    // ======================================================
    //  COMMAND
    // ======================================================
    /// <summary>
    /// Represents the GetDependentCountsQuery application component.
    /// </summary>
    public class GetDependentCountsQuery
        : IRequest<ApiResponse<GetDependentsDetailResponseDTO>>
    {
        public GetDependentRequestDTO DTO { get; set; }

        public GetDependentCountsQuery(GetDependentRequestDTO dto)
        {
            DTO = dto;
        }
    }

    // ======================================================
    //  HANDLER
    // ======================================================
    /// <summary>
    /// Handles GetDependentCountsQuery requests.
    /// </summary>
        #endregion

    #region Handler

public class GetDependentCountsQueryHandler
        : IRequestHandler<GetDependentCountsQuery, ApiResponse<GetDependentsDetailResponseDTO>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetDependentCountsQueryHandler> _logger;
        private readonly IPermissionService _permissionService;
        private readonly IIdEncoderService _idEncoderService;
        private readonly ICommonRequestService _commonRequestService;
        private readonly IFileStorageService _fileStorageService;

        public GetDependentCountsQueryHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetDependentCountsQueryHandler> logger,
            IPermissionService permissionService,
            IIdEncoderService idEncoderService,
            ICommonRequestService commonRequestService,
            IFileStorageService fileStorageService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _permissionService = permissionService;
            _idEncoderService = idEncoderService;
            _commonRequestService = commonRequestService;
            _fileStorageService = fileStorageService;
        }

        public async Task<ApiResponse<GetDependentsDetailResponseDTO>> Handle(
            GetDependentCountsQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("🔹 GetDependentCounts started");

                // ===============================
                //  STEP 1: COMMON VALIDATION
                // ===============================
                var validation = await _commonRequestService.ValidateInstitutionUserRequestAsync();

                if (!validation.Success)
                    throw new UnauthorizedAccessException(validation.ErrorMessage);

                // ===============================
                //  STEP 2: DECODE STAFF_MEMBER ID
                // ===============================


                // ===============================
                //  STEP 4: REPOSITORY CALL
                // ===============================
                var result = await _unitOfWork
                    .StaffMemberDependentRepository
                    .GetDetailInfo(validation.LoggedInStaffMemberId, request.DTO);

                if (result == null)
                    result = new GetDependentsDetailResponseDTO();

                // ===============================
                //  STEP 5: COMMIT
                // ===============================
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("✅ GetDependentCounts success");

                // ===============================
                //  STEP 6: RESPONSE
                // ===============================
                return ApiResponse<GetDependentsDetailResponseDTO>
                    .Success(result, "Dependent details fetched successfully.");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();

                _logger.LogError(
                    ex,
                    "❌ Error while fetching dependent details. StaffMemberId: {StaffMemberId}",
                    request.DTO?.StaffMemberId);

                throw; //  As per your architecture rules
            }
        }
    }
    #endregion
}
