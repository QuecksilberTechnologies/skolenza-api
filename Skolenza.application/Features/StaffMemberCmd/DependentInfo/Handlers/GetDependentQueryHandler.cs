// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Retrieves dependent information and delegates failures to middleware.
// ================================================================

using skolenza.application.Common.Helpers.ProjectionHelpers.Staff;
using skolenza.application.Common.Helpers.RequestHelper;
using skolenza.application.DTOS.Staff.Dependent;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.IFileStorage;
using skolenza.application.Interfaces.IPermission;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.StaffMemberCmd.DependentInfo.Handlers
{

    #region Query

    /// <summary>
    /// Represents the GetDependentInfoQuery application component.
    /// </summary>
    public class GetDependentInfoQuery : IRequest<ApiResponse<List<GetDependentResponseDTO>>>
    {
        public GetDependentRequestDTO DTO { get; set; }

        public GetDependentInfoQuery(GetDependentRequestDTO dto)
        {
            DTO = dto;
        }

    }
    /// <summary>
    /// Handles GetDependentInfoQuery requests.
    /// </summary>
        #endregion

    #region Handler

public class GetDependentInfoQueryHandler
   : IRequestHandler<GetDependentInfoQuery, ApiResponse<List<GetDependentResponseDTO>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetDependentInfoQueryHandler> _logger;
        private readonly IPermissionService _permissionService;
        private readonly IIdEncoderService _idEncoderService;
        private readonly ICommonRequestService _commonRequestService;
        private readonly IConfiguration _configuration;
        private readonly IFileStorageService _fileStorageService;

        public GetDependentInfoQueryHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetDependentInfoQueryHandler> logger,
            IPermissionService permissionService,
            IIdEncoderService idEncoderService,
            ICommonRequestService commonRequestService,
            IConfiguration configuration, IFileStorageService fileStorageService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _permissionService = permissionService;
            _idEncoderService = idEncoderService;
            _commonRequestService = commonRequestService;
            _configuration = configuration   ;
            _fileStorageService = fileStorageService;
        }

        public async Task<ApiResponse<List<GetDependentResponseDTO>>> Handle(
           GetDependentInfoQuery request,
           CancellationToken cancellationToken)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                //  STEP 1: COMMON VALIDATION (SAME AS CONTACT)
                var validation =
                    await _commonRequestService.ValidateInstitutionUserRequestAsync();

                if (!validation.Success)
                throw new UnauthorizedAccessException(validation.ErrorMessage);

                //  STEP 2: Assign decoded props
                var staffMemberId = RequestCommonHelper.DecodeOnlyStaffMemberId(
                    request.DTO.StaffMemberId,
                    validation.Claims.InstitutionEncriptionKey,
                    _idEncoderService);

                //  STEP 4: Repository call
                var result =
                    await _unitOfWork.StaffMemberDependentRepository
                        .GetInfo(staffMemberId, request.DTO);

                if (result == null || !result.Data.Any())
                {
                    return ApiResponse<List<GetDependentResponseDTO>>
                      .SuccessPaginatedPercentage(
                          Data: null,
                          Message: "Dependent info not found.",
                          PageNumber: 1,
                          PageSize: 1,
                          TotalRecords: 0,
                          TotalPages: 0,
                          CompletionPercentage:
                             0,
                          HasUploadedAll: false
                      );
                }

                //  STEP 5: Projection + Encryption + FilePath + Completion %
                var responseDTO =
                    ProjectionHelper.ToGetDependentResponseDTOs(
                        result.Data,
                        _idEncoderService,
                        validation.Claims.InstitutionEncriptionKey,
                        _configuration, _fileStorageService
                    );

                await _unitOfWork.CommitTransactionAsync();

                //  STEP 6: Response (same style as Contact)
                return ApiResponse<List<GetDependentResponseDTO>>
                    .SuccessPaginatedPercentage(
                        Data: responseDTO,
                        Message: "Dependent info retrieved successfully.",
                        PageNumber: result.PageNumber,
                        PageSize: result.PageSize,
                        TotalRecords: result.TotalCount,
                        TotalPages: result.TotalPages,
                        CompletionPercentage: result.Data.Any()
                            ? Math.Round(responseDTO.Average(x => x.CompletionPercentage), 0)
                            : 0,
                        HasUploadedAll: null
                    );
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();

                _logger.LogError(
                    ex,
                    "Error occurred while fetching Dependent info for StaffMemberId: {StaffMemberId}",
                    request.DTO?.StaffMemberId);

                throw;
            }
        }
    }


    #endregion
}
