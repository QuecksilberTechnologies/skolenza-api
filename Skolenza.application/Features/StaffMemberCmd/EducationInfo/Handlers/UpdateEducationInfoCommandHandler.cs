// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles Update Education Info Command Handler requests.
// ================================================================

using AutoMapper;
using skolenza.application.Common.Helpers;
using skolenza.application.Constants;
using skolenza.application.DTOS.Common;
using skolenza.application.DTOS.Staff.Education;
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
using System.Security.Principal;

namespace skolenza.application.Features.StaffMemberCmd.EducationInfo.Handlers
{

    #region Command

    /// <summary>
    /// Represents the UpdateEducationInfoCommand application component.
    /// </summary>
    public class UpdateEducationInfoCommand : IRequest<ApiResponse<bool>>
    {
        public UpdateEducationRequestDTO DTO { get; set; }
        public UpdateEducationInfoCommand(UpdateEducationRequestDTO dto) => DTO = dto;
    }


    /// <summary>
    /// Handles UpdateEducationInfoCommand requests.
    /// </summary>
        #endregion

    #region Handler

public class UpdateEducationInfoCommandHandler : IRequestHandler<UpdateEducationInfoCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<UpdateEducationInfoCommandHandler> _logger;
        private readonly ITokenService _tokenService;
        private readonly IPermissionService _permissionService;
        private readonly IConfiguration _config;
        private readonly IEncryptionService _encryptionService;
        private readonly IFileStorageService _fileStorageService;
        private readonly IIdEncoderService _idEncoderService;
        private readonly ICommonRequestService _commonRequestService;


        public UpdateEducationInfoCommandHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor,
            ILogger<UpdateEducationInfoCommandHandler> logger,
            ITokenService tokenService,
            IPermissionService permissionService,
            IConfiguration config,
            IEncryptionService encryptionService, IIdEncoderService idEncoderService, IFileStorageService fileStorageService, ICommonRequestService commonRequestService)
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
            _commonRequestService = commonRequestService;
        }


        public async Task<ApiResponse<bool>> Handle(
       UpdateEducationInfoCommand request,
       CancellationToken cancellationToken)
        {
            string? uploadedFileKey = null;

            try
            {
                _logger.LogInformation("UpdateEducation started");

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
                // 4️⃣ FETCH EXISTING
                // ===============================
                var existing =
                    await _unitOfWork.StaffMemberEducationRepository
                        .GetSingleRecordAsync(request.DTO.Id, true);

                if (existing == null)
                    throw new ApiException("Education record not found.", 404);

                if (!await _commonRequestService.CanAccessStaffMemberDataAsync(
                        validation, existing.StaffMemberId, StaffMemberDataAccessRequirement.PersonalDetails, cancellationToken))
                    throw new ForbiddenAccessException(AppConstants.ErrorMessages.PermissionDenied);

                if (validation.RoleTypeId != ConstantValues.RoleTypeAdmin &&
                    (existing.IsInfoVerified == true || existing.IsEditAllowed != true))
                    throw new ForbiddenAccessException(AppConstants.ErrorMessages.PermissionDenied);

                var effectiveStartDate = request.DTO.StartDate.HasValue
                    ? DateOnly.FromDateTime(request.DTO.StartDate.Value)
                    : existing.StartDate;
                var effectiveEndDate = request.DTO.EndDate.HasValue
                    ? DateOnly.FromDateTime(request.DTO.EndDate.Value)
                    : existing.EndDate;

                StaffMemberProfileValidationHelper.RequireText(
                    string.IsNullOrWhiteSpace(request.DTO.Degree) ? existing.Degree : request.DTO.Degree,
                    "Degree");
                StaffMemberProfileValidationHelper.RequireText(
                    string.IsNullOrWhiteSpace(request.DTO.InstituteName)
                        ? existing.InstituteName
                        : request.DTO.InstituteName,
                    "Institute name");
                if (existing.ScoreType <= 0 &&
                    (string.IsNullOrWhiteSpace(request.DTO.ScoreType) ||
                     !int.TryParse(request.DTO.ScoreType, out var requestedScoreType) ||
                     requestedScoreType <= 0))
                {
                    throw new ValidationErrorException("Score type is required.");
                }
                StaffMemberProfileValidationHelper.ValidateDateRange(
                    effectiveStartDate?.ToDateTime(TimeOnly.MinValue),
                    effectiveEndDate?.ToDateTime(TimeOnly.MinValue));
                StaffMemberProfileValidationHelper.ValidateGap(
                    request.DTO.IsEducationGapBeforeDegree == true,
                    request.DTO.GapYears,
                    request.DTO.ReasonOfEducationGap,
                    "Education gap");

                // ===============================
                // 5️⃣ START TRANSACTION
                // ===============================
                await _unitOfWork.BeginTransactionAsync();

                var dto = request.DTO;

                // ===============================
                // 6️⃣ APPLY UPDATES
                // ===============================
                if (!string.IsNullOrWhiteSpace(dto.Degree))
                    existing.Degree = dto.Degree.Trim();

                if (!string.IsNullOrWhiteSpace(dto.InstituteName))
                    existing.InstituteName = dto.InstituteName.Trim();

                if (!string.IsNullOrWhiteSpace(dto.ScoreValue))
                    existing.ScoreValue = dto.ScoreValue.Trim();

                if (!string.IsNullOrWhiteSpace(dto.ScoreType) &&
                    int.TryParse(dto.ScoreType.Trim(), out int parsedScoreType))
                    existing.ScoreType = parsedScoreType;

                if (!string.IsNullOrWhiteSpace(dto.GradeDivision))
                    existing.GradeDivision = dto.GradeDivision.Trim();

                existing.EducationGap = dto.IsEducationGapBeforeDegree;
                existing.GapYears = dto.GapYears;
                existing.ReasonOfEducationGap = dto.ReasonOfEducationGap?.Trim();
                existing.StartDate = effectiveStartDate;
                existing.EndDate = effectiveEndDate;


                // ===============================
                // 7️⃣ FILE UPLOAD
                // ===============================
                if (dto.EducationDocument != null &&
                    dto.EducationDocument.Length > 0)
                {
                    try
                    {
                        string degreeName =
                            dto.Degree?.Trim().ToLower().Replace(" ", "_") ?? "doc";

                        string newFileName =
                            $"{ConstantValues.EducationFolder}-{existing.StaffMemberId}-{degreeName}-{DateTime.UtcNow:yyyyMMddHHmmss}";

                        string folderPath =
                            $"{ConstantValues.InstitutionFolder}-{validation.InstitutionId}/" +
                            $"{ConstantValues.StaffMemberFolder}/{existing.StaffMemberId}/" +
                            $"{ConstantValues.EducationFolder}";

                        uploadedFileKey = await _fileStorageService.UploadFileAsync(
                            dto.EducationDocument,
                            folderPath,
                            newFileName);

                        existing.FilePath = uploadedFileKey;
                        existing.FileName = newFileName;
                        existing.HasEducationDocUploded = true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Education file upload failed");
                        throw new ApiException("File upload failed.", 500);
                    }
                }

                // ===============================
                //  AUDIT
                // ===============================
                existing.UpdatedById = validation.UserStaffMemberId;
                existing.UpdatedDateTime = DateTime.UtcNow;

                // ===============================
                // 8️⃣ SAVE
                // ===============================
                await _unitOfWork.StaffMemberEducationRepository
                    .UpdateStaffMemberFieldAsync(existing);

                // ===============================
                // 9️⃣ COMMIT
                // ===============================
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("UpdateEducation success");

                return ApiResponse<bool>.Success(true, "Education updated successfully.");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();

                _logger.LogError(ex, "Error updating education");

                //  FILE CLEANUP
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
