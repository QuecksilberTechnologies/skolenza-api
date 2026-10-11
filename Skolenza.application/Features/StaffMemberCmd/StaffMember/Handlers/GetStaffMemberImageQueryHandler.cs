// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles Get StaffMember Image Query Handler requests.
// ================================================================

using skolenza.application.Common.Helpers.RequestHelper;
using skolenza.application.Constants;
using skolenza.application.DTOS.Staff.BaseStaffMember;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.IFileStorage;
using skolenza.application.Interfaces.IPermission;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.StaffMemberCmd.StaffMemberBase.Handlers
{

    #region Query

    /// <summary>
    /// Represents the GetStaffMemberImageQuery application component.
    /// </summary>
    public class GetStaffMemberImageQuery : IRequest<ApiResponse<GetStaffMemberImageReponseDTO>>
    {
        public GetStaffMemberImageRequestDTO DTO { get; }

        public GetStaffMemberImageQuery(GetStaffMemberImageRequestDTO dTO)
        {
            DTO = dTO;
        }
    }


    /// <summary>
    /// Handles GetStaffMemberImageQuery requests.
    /// </summary>
        #endregion

    #region Handler

public class GetStaffMemberImageQueryHandler
     : IRequestHandler<GetStaffMemberImageQuery, ApiResponse<GetStaffMemberImageReponseDTO>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetStaffMemberImageQueryHandler> _logger;
        private readonly IPermissionService _permissionService;
        private readonly ICommonRequestService _commonRequestService;
        private readonly IConfiguration _config;
        private readonly IIdEncoderService _idEncoderService;
        private readonly IFileStorageService _fileStorageService;
        public GetStaffMemberImageQueryHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetStaffMemberImageQueryHandler> logger,
            IPermissionService permissionService,
            ICommonRequestService commonRequestService,
            IConfiguration config,
            IIdEncoderService idEncoderService,
            IFileStorageService fileStorageService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _permissionService = permissionService;
            _commonRequestService = commonRequestService;
            _config = config;
            _idEncoderService = idEncoderService;
            _fileStorageService = fileStorageService;
        }

        public async Task<ApiResponse<GetStaffMemberImageReponseDTO>> Handle(
     GetStaffMemberImageQuery request,
     CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("GetStaffMemberImage started");

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

                if (!await _commonRequestService.CanAccessStaffMemberDataAsync(
                        validation,
                        staffMemberId,
                        StaffMemberDataAccessRequirement.DirectoryBasic,
                        cancellationToken))
                    throw new ForbiddenAccessException(AppConstants.ErrorMessages.PermissionDenied);
                // 4️⃣ FETCH IMAGE
                // ===============================
                var image =
                    await _unitOfWork.StaffMembers.GetImage(
                        validation.InstitutionId,
                        staffMemberId,
                        request.DTO);

                if (image == null)
                {
                    _logger.LogInformation(
                        "No primary image found | StaffMemberId={EmpId}",
                        staffMemberId);

                    throw new ApiException("StaffMember image not found.", 404);
                }

                // ===============================
                // 5️⃣ S3 → SIGNED URL
                // ===============================
                if (!string.IsNullOrWhiteSpace(image.FilePath))
                {
                    image.FilePath =
                        _fileStorageService.GetFileUrl(image.FilePath);
                    _logger.LogInformation("Image-Path : {image.FilePath} ", image.FilePath);
                }

                image.StaffMemberId = request.DTO.StaffMemberId;

                _logger.LogInformation("GetStaffMemberImage success");

                return ApiResponse<GetStaffMemberImageReponseDTO>
                    .Success(image, "StaffMember image fetched successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error fetching staffMember image | StaffMemberId: {StaffMemberId}",
                    request.DTO?.StaffMemberId);

                throw; //  MUST
            }
        }
    }

    #endregion
}
