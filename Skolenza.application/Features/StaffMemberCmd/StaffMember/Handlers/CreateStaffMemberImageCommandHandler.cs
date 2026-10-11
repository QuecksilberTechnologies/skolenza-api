// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles Create StaffMember Image Command Handler requests.
// ================================================================

using AutoMapper;
using skolenza.application.Common.Helpers.EncryptionHelper;
using skolenza.application.DTOs.Module;
using skolenza.application.DTOS.Staff.BaseStaffMember;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IFileStorage;
using skolenza.application.Interfaces.IPermission;
using skolenza.application.Wrappers;

using skolenza.domain.Entity;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace skolenza.application.Features.StaffMemberCmd.StaffMemberBase.Handlers
{

    #region Command

    /// <summary>
    /// Represents the CreateStaffMemberImageCommand application component.
    /// </summary>
    public class CreateStaffMemberImageCommand : IRequest<ApiResponse<GetStaffMemberImageReponseDTO>>
    {
        public CreateStaffMemberImageRequestDTO DTO { get; set; }

        public CreateStaffMemberImageCommand(CreateStaffMemberImageRequestDTO dto)
        {
            DTO = dto;
        }
    }

    /// <summary>
    /// Handles CreateStaffMemberImageCommand requests.
    /// </summary>
        #endregion

    #region Handler

public class CreateStaffMemberImageCommandHandler
      : IRequestHandler<CreateStaffMemberImageCommand, ApiResponse<GetStaffMemberImageReponseDTO>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<CreateStaffMemberImageCommandHandler> _logger;
        private readonly IFileStorageService _fileStorageService;
        private readonly ICommonRequestService _commonRequestService;
        private readonly IPermissionService _permissionService;
        private readonly IConfiguration _configuration;

        public CreateStaffMemberImageCommandHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<CreateStaffMemberImageCommandHandler> logger,
            IFileStorageService fileStorageService,
            ICommonRequestService commonRequestService,
            IPermissionService permissionService,
            IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _fileStorageService = fileStorageService;
            _commonRequestService = commonRequestService;
            _permissionService = permissionService;
            _configuration = configuration;
        }

        public async Task<ApiResponse<GetStaffMemberImageReponseDTO>> Handle(
      CreateStaffMemberImageCommand request,
      CancellationToken cancellationToken)
        {
            string? uploadedFileKey = null;

            try
            {
                _logger.LogInformation("CreateStaffMemberImage started");

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

                var staffMemberId = validation.LoggedInStaffMemberId;
                if (staffMemberId <= 0)
                    throw new ValidationErrorException("Invalid StaffMemberId.");
                // 4️⃣ START TRANSACTION
                // ===============================
                await _unitOfWork.BeginTransactionAsync();

                // ===============================
                // 5️⃣ FILE UPLOAD
                // ===============================
                string? filePath = null;
                string? fileName = null;
                bool hasImageUploaded = false;

                if (request.DTO.ImageFile != null &&
                    request.DTO.ImageFile.Length > 0 &&
                    request.DTO.IsActive)
                {
                    try
                    {
                        fileName =
                            $"profile-{staffMemberId}-{DateTime.UtcNow:yyMMddHHmmss}";

                        string folderPath =
                            $"institutions/institution-{validation.InstitutionId}/staffMembers/{staffMemberId}/profile";

                        uploadedFileKey =
                            await _fileStorageService.UploadFileAsync(
                                request.DTO.ImageFile,
                                folderPath,
                                fileName);

                        if (!string.IsNullOrWhiteSpace(uploadedFileKey))
                        {
                            filePath = uploadedFileKey;
                            hasImageUploaded = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "StaffMember image upload failed");
                        throw new ApiException("Image upload failed.", 500);
                    }
                }

                // ===============================
                // 6️⃣ MAP ENTITY
                // ===============================
                var entity = _mapper.Map<StaffMemberImage>(request.DTO);

                entity.StaffMemberId = staffMemberId;
                entity.AddedById = validation.UserStaffMemberId;
                entity.AddedDateTime = DateTime.UtcNow;
                entity.IsActive = request.DTO.IsActive;
                entity.HasImageUploaded = hasImageUploaded;
                entity.FileName = hasImageUploaded ? fileName : null;
                entity.FilePath = hasImageUploaded ? filePath : null;
                entity.FileType = hasImageUploaded ? 1 : 0;
                entity.IsPrimary = true;

                // ===============================
                // 7️⃣ SAVE
                // ===============================
                var savedImage =
                    await _unitOfWork.StaffMembers.AddImageAsync(entity);

                if (savedImage == null)
                    throw new ApiException("StaffMember image creation failed.", 500);

                // ===============================
                // 8️⃣ BUILD FULL URL
                // ===============================
                string baseUrl =
                    _configuration["FileSettings:BaseUrl"] ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(savedImage.FilePath))
                    savedImage.FilePath = $"{baseUrl}{savedImage.FilePath}";

                // ===============================
                // 9️⃣ COMMIT
                // ===============================
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("CreateStaffMemberImage success");

                return ApiResponse<GetStaffMemberImageReponseDTO>
                    .Success(savedImage, "StaffMember image added successfully.");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();

                _logger.LogError(ex, "CreateStaffMemberImage failed");

                //  FILE CLEANUP (CRITICAL )
                if (!string.IsNullOrEmpty(uploadedFileKey))
                {
                    try
                    {
                        await _fileStorageService.DeleteFileAsync(uploadedFileKey);
                    }
                    catch (Exception cleanupEx)
                    {
                        _logger.LogError(cleanupEx, "Failed to cleanup uploaded image");
                    }
                }

                throw; //  MUST
            }
        }
    }
    #endregion
}
