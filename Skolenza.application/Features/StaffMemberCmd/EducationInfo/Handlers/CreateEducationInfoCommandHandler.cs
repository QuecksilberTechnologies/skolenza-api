// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles Create Education Info Command Handler requests.
// ================================================================

using AutoMapper;
using skolenza.application.Common.Helpers;
using skolenza.application.Common.Helpers.ProjectionHelpers.Staff;
using skolenza.application.Common.Helpers.RequestHelper;
using skolenza.application.Constants;
using skolenza.application.DTOS.Staff.Education;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.IFileStorage;
using skolenza.application.Interfaces.IPermission;
using skolenza.application.Interfaces.ITokenService;
using skolenza.application.Wrappers;

using skolenza.domain.Entity;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.StaffMemberCmd.EducationInfo.Handlers
{

    #region Command

    /// <summary>
    /// Represents the CreateEducationInfoCommand application component.
    /// </summary>
    public class CreateEducationInfoCommand : IRequest<ApiResponse<List<GetEducationResponseDTO>>>
    {
        public CreateEducationRequestDTO DTO { get; }

        public CreateEducationInfoCommand(CreateEducationRequestDTO dto)
        {
            DTO = dto;
        }
    }

    /// <summary>
    /// Handles CreateEducationInfoCommand requests.
    /// </summary>
        #endregion

    #region Handler

public class CreateEducationInfoCommandHandler : IRequestHandler<CreateEducationInfoCommand, ApiResponse<List<GetEducationResponseDTO>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<CreateEducationInfoCommandHandler> _logger;
        private readonly ITokenService _tokenService;
        private readonly IPermissionService _permissionService;
        private readonly IConfiguration _config;
        private readonly IEncryptionService _encryptionService;
        private readonly IIdEncoderService _idEncoderService;
        private readonly IFileStorageService _fileStorageService;
        private readonly IConfiguration _configuration;
        private readonly ICommonRequestService _commonRequestService;


        public CreateEducationInfoCommandHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor,
            ILogger<CreateEducationInfoCommandHandler> logger,
            ITokenService tokenService,
            IPermissionService permissionService,
            IConfiguration config,
            IEncryptionService encryptionService,
            IIdEncoderService idEncoderService,
            IFileStorageService fileStorageService, IConfiguration configuration, ICommonRequestService commonRequestService)
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
            _fileStorageService = fileStorageService;
            _configuration = configuration;
            _commonRequestService = commonRequestService;
        }

        public async Task<ApiResponse<List<GetEducationResponseDTO>>> Handle(
      CreateEducationInfoCommand request,
      CancellationToken cancellationToken)
        {
            string? uploadedFileKey = null;

            try
            {
                _logger.LogInformation("CreateEducation started");

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

                StaffMemberProfileValidationHelper.RequireText(request.DTO.Degree, "Degree");
                StaffMemberProfileValidationHelper.RequireText(request.DTO.InstituteName, "Institute name");
                StaffMemberProfileValidationHelper.RequireText(request.DTO.ScoreType, "Score type");
                if (!int.TryParse(request.DTO.ScoreType?.Trim(), out var scoreType) || scoreType <= 0)
                    throw new ValidationErrorException("Invalid score type.");

                StaffMemberProfileValidationHelper.ValidateDateRange(
                    request.DTO.StartDate,
                    request.DTO.EndDate);
                StaffMemberProfileValidationHelper.ValidateGap(
                    request.DTO.IsEducationGapBeforeDegree,
                    request.DTO.GapYears,
                    request.DTO.ReasonOfEducationGap,
                    "Education gap");
                // 4️⃣ START TRANSACTION
                // ===============================
                await _unitOfWork.BeginTransactionAsync();

                // ===============================
                // 5️⃣ FILE UPLOAD
                // ===============================
                string? docPath = null;
                string? fileName = null;
                bool hasEducationUploaded = false;

                if (request.DTO.EducationDocument != null &&
                    request.DTO.EducationDocument.Length > 0)
                {
                    try
                    {
                        string degreeName =
                            request.DTO.Degree?.Trim().ToLower().Replace(" ", "_") ?? "doc";

                        fileName =
                            $"{ConstantValues.EducationFolder}-{staffMemberId}-{degreeName}-{DateTime.UtcNow:yyyyMMddHHmmss}";

                        string folderPath =
                            $"{ConstantValues.InstitutionFolder}-{validation.InstitutionId}/" +
                            $"{ConstantValues.StaffMemberFolder}/{staffMemberId}/" +
                            $"{ConstantValues.EducationFolder}";

                        uploadedFileKey = await _fileStorageService.UploadFileAsync(
                            request.DTO.EducationDocument,
                            folderPath,
                            fileName);

                        if (!string.IsNullOrWhiteSpace(uploadedFileKey))
                        {
                            docPath = uploadedFileKey;
                            hasEducationUploaded = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Education file upload failed");
                        throw new ApiException("File upload failed.", 500);
                    }
                }

                // ===============================
                // 6️⃣ MAP ENTITY
                // ===============================
                var educationEntity = _mapper.Map<StaffMemberEducation>(request.DTO);

                educationEntity.StaffMemberId = staffMemberId;
                educationEntity.ScoreType = scoreType;
                educationEntity.AddedById = validation.LoggedInStaffMemberId;
                educationEntity.AddedDateTime = DateTime.UtcNow;

                educationEntity.IsActive = true;
                educationEntity.IsInfoVerified = false;
                educationEntity.IsEditAllowed = true;

                educationEntity.FileType = hasEducationUploaded ? 2 : 0;
                educationEntity.FilePath = docPath;
                educationEntity.FileName = fileName;
                educationEntity.HasEducationDocUploded = hasEducationUploaded;

                // ===============================
                // 7️⃣ SAVE
                // ===============================
                var responseDTO =
                    await _unitOfWork.StaffMemberEducationRepository
                        .CreateAsync(educationEntity);

                if (responseDTO == null)
                    throw new ApiException("Failed to create education record.", 500);

                // ===============================
                // 8️⃣ PROJECTION
                // ===============================
                var encryptedList =
                    ProjectionHelper.ToGetEducationResponseDTOs(
                        responseDTO,
                        _idEncoderService,
                        validation.Claims.InstitutionEncriptionKey,
                        _configuration , _fileStorageService);

                // ===============================
                // 9️⃣ COMMIT
                // ===============================
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("CreateEducation success");

                return ApiResponse<List<GetEducationResponseDTO>>
                    .Success(encryptedList, "Education info created successfully.");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();

                _logger.LogError(ex, "Error creating education");

                //  FILE CLEANUP (S3)
                if (!string.IsNullOrEmpty(uploadedFileKey))
                {
                    try
                    {
                        await _fileStorageService.DeleteFileAsync(uploadedFileKey);
                    }
                    catch (Exception cleanupEx)
                    {
                        _logger.LogError(cleanupEx, "File cleanup failed");
                    }
                }

                throw; //  MUST
            }
        }

    }
    #endregion
}
