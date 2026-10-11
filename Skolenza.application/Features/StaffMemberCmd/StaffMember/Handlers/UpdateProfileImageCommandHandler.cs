// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles Update Profile Image Command Handler requests.
// ================================================================

using skolenza.application.Constants;
using skolenza.application.DTOS.Staff.BaseStaffMember;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.IFileStorage;
using skolenza.application.Interfaces.IPermission;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Interfaces.ITokenService;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.StaffMemberCmd.StaffMemberBase.Handlers
{

    #region Command

    /// <summary>
    /// Represents the UpdateProfileImageCommand application component.
    /// </summary>
    public class UpdateProfileImageCommand : IRequest<ApiResponse<bool>>
    {
        public UpdateStaffMemberImageRequestDTO DTO { get; set; }
        public UpdateProfileImageCommand(UpdateStaffMemberImageRequestDTO dto) => DTO = dto;
    }

    /// <summary>
    /// Handles UpdateIdentityInfoCommand requests.
    /// </summary>
        #endregion

    #region Handler

public class UpdateIdentityInfoCommandHandler
        : IRequestHandler<UpdateProfileImageCommand, ApiResponse<bool>>
    {
        private readonly IBaseStaffMemberRepository _staffMemberRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UpdateIdentityInfoCommandHandler> _logger;
        private readonly ITokenService _tokenService;
        private readonly IPermissionService _permissionService;
        private readonly IFileStorageService _fileStorageService;
        private readonly IConfiguration _config;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IEncryptionService _encryptionService;
        private readonly IIdEncoderService _idEncoderService;
        private readonly ICommonRequestService _commonRequestService;


        public UpdateIdentityInfoCommandHandler(
            IBaseStaffMemberRepository staffMemberRepository,
            IUnitOfWork unitOfWork,
            ILogger<UpdateIdentityInfoCommandHandler> logger,
            ITokenService tokenService,
            IPermissionService permissionService,
            IConfiguration config,
            IHttpContextAccessor httpContextAccessor,
            IEncryptionService encryptionService,
            IFileStorageService fileStorageService,
            IIdEncoderService idEncoderService,
            ICommonRequestService commonRequestService)
        {
            _staffMemberRepository = staffMemberRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _tokenService = tokenService;
            _permissionService = permissionService;
            _config = config;
            _httpContextAccessor = httpContextAccessor;
            _encryptionService = encryptionService;
            _fileStorageService = fileStorageService;
            _idEncoderService = idEncoderService;
            _commonRequestService = commonRequestService;
        }

        public async Task<ApiResponse<bool>> Handle(
       UpdateProfileImageCommand request,
       CancellationToken cancellationToken)
        {
            string? uploadedFileKey = null;

            try
            {
                _logger.LogInformation("UpdateProfileImage started");

                // ===============================
                // 1️⃣ VALIDATION
                // ===============================
                var validation =
                    await _commonRequestService.ValidateInstitutionUserRequestAsync();

                if (!validation.Success)
                    throw new UnauthorizedAccessException(validation.ErrorMessage);

                if (request?.DTO == null)
                    throw new ValidationErrorException("Invalid request.");
                // 3️⃣ FETCH EXISTING
                // ===============================
                var staffMemberImageInfo =
                    await _staffMemberRepository.IsImageExist(request.DTO.Id, true);

                if (staffMemberImageInfo == null)
                    throw new ApiException("StaffMember image record not found.", 404);

                // ===============================
                // 4️⃣ START TRANSACTION
                // ===============================
                await _unitOfWork.BeginTransactionAsync();

                string folderPath =
                    $"{ConstantValues.InstitutionFolder}-{validation.InstitutionId}/" +
                    $"{ConstantValues.StaffMemberFolder}/{staffMemberImageInfo.StaffMemberId}/" +
                    $"{ConstantValues.ProfileFolder}";

                // ======================================================
                //  CASE 1: DEACTIVATE (DELETE IMAGE)
                // ======================================================
                if (!request.DTO.IsActive)
                {
                    if (staffMemberImageInfo.HasImageUploaded&&
                        !string.IsNullOrWhiteSpace(staffMemberImageInfo.FilePath))
                    {
                        await _fileStorageService.DeleteFileAsync(staffMemberImageInfo.FilePath);
                    }

                    staffMemberImageInfo.HasImageUploaded = false;
                    staffMemberImageInfo.FileName = null;
                    staffMemberImageInfo.FilePath = null;
                    staffMemberImageInfo.UpdateById = validation.UserStaffMemberId;
                    staffMemberImageInfo.UpdatedDateTime = DateTime.UtcNow;

                    var status =
                        await _unitOfWork.StaffMembers.UpdateProfileImage(staffMemberImageInfo);

                    if (!status)
                        throw new ApiException("Failed to deactivate profile image.", 500);

                    await _unitOfWork.CommitTransactionAsync();

                    return ApiResponse<bool>.Success(true, "Profile image disabled.");
                }

                // ======================================================
                //  CASE 2: UPDATE / REPLACE IMAGE
                // ======================================================
                if (request.DTO.ProfileImage != null &&
                    request.DTO.ProfileImage.Length > 0)
                {
                    // Delete old
                    if (staffMemberImageInfo.HasImageUploaded &&
                        !string.IsNullOrWhiteSpace(staffMemberImageInfo.FilePath))
                    {
                        await _fileStorageService.DeleteFileAsync(staffMemberImageInfo.FilePath);
                    }

                    string newFileName =
                        $"{ConstantValues.ProfileFolder}-{staffMemberImageInfo.StaffMemberId}-{DateTime.UtcNow:yyMMddHHmmss}";

                    uploadedFileKey =
                        await _fileStorageService.UploadFileAsync(
                            request.DTO.ProfileImage,
                            folderPath,
                            newFileName);

                    staffMemberImageInfo.FileName = newFileName;
                    staffMemberImageInfo.FilePath = uploadedFileKey;
                    staffMemberImageInfo.HasImageUploaded = true;
                    staffMemberImageInfo.UpdateById = validation.UserStaffMemberId;
                    staffMemberImageInfo.UpdatedDateTime = DateTime.UtcNow;
                }

                // ===============================
                // 5️⃣ SAVE
                // ===============================
                var updateStatus =
                    await _unitOfWork.StaffMembers.UpdateProfileImage(staffMemberImageInfo);

                if (!updateStatus)
                    throw new ApiException("Failed to update profile image.", 500);

                // ===============================
                // 6️⃣ COMMIT
                // ===============================
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("UpdateProfileImage success");

                return ApiResponse<bool>.Success(true, "Profile image updated successfully.");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();

                _logger.LogError(ex, "UpdateProfileImage failed");

                //  FILE CLEANUP (CRITICAL )
                if (!string.IsNullOrEmpty(uploadedFileKey))
                {
                    try
                    {
                        await _fileStorageService.DeleteFileAsync(uploadedFileKey);
                    }
                    catch (Exception cleanupEx)
                    {
                        _logger.LogError(cleanupEx, "Failed to cleanup uploaded file");
                    }
                }

                throw; //  MUST
            }
        }
    }


    #endregion
}
