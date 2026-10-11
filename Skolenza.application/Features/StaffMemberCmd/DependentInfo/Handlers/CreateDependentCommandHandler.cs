// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles Create Dependent Command Handler requests.
// ================================================================

using AutoMapper;
using skolenza.application.Common.Enums;
using skolenza.application.Common.Helpers.ProjectionHelpers.Staff;
using skolenza.application.Common.Helpers.RequestHelper;
using skolenza.application.Constants;
using skolenza.application.DTOs.Module;
using skolenza.application.DTOS.Staff.Dependent;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.IFileStorage;
using skolenza.application.Interfaces.IPermission;
using skolenza.application.Wrappers;

using skolenza.domain.Entity;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace skolenza.application.Features.StaffMemberCmd.DependentInfo.Handlers
{

    #region Command

    /// <summary>
    /// Represents the CreateDependentCommand application component.
    /// </summary>
    public class CreateDependentCommand : IRequest<ApiResponse<List<GetDependentResponseDTO>>>
    {
        public CreateDependentRequestDTO DTO { get; set; }

        public CreateDependentCommand(CreateDependentRequestDTO dto)
        {
            DTO = dto;
        }

    }

    /// <summary>
    /// Handles CreateDependentCommand requests.
    /// </summary>
        #endregion

    #region Handler

public class CreateDependentCommandHandler
    : IRequestHandler<CreateDependentCommand, ApiResponse<List<GetDependentResponseDTO>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<CreateDependentCommandHandler> _logger;
        private readonly IPermissionService _permissionService;
        private readonly IIdEncoderService _idEncoderService;
        private readonly IFileStorageService _fileStorageService;
        private readonly ICommonRequestService _commonRequestService;
        private readonly IConfiguration _configuration;


        public CreateDependentCommandHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<CreateDependentCommandHandler> logger,
            IPermissionService permissionService,
            IIdEncoderService idEncoderService,
            IFileStorageService fileStorageService,
            ICommonRequestService commonRequestService, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _permissionService = permissionService;
            _idEncoderService = idEncoderService;
            _fileStorageService = fileStorageService;
            _commonRequestService = commonRequestService;
            _configuration = configuration;
        }

        public async Task<ApiResponse<List<GetDependentResponseDTO>>> Handle(
      CreateDependentCommand request,
      CancellationToken cancellationToken)
        {
            string? uploadedFileKey = null;

            try
            {
                _logger.LogInformation("CreateDependent started");

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

                skolenza.application.Common.Helpers.StaffMemberProfileValidationHelper.RequireText(
                    request.DTO.DependentName,
                    "Dependent name");
                if (!Enum.IsDefined(typeof(RelationDependant), request.DTO.Relation))
                    throw new ValidationErrorException("Invalid dependent relation.");
                skolenza.application.Common.Helpers.StaffMemberProfileValidationHelper.ValidateDateOfBirth(
                    request.DTO.DateOfBirth);
                // 4️⃣ START TRANSACTION
                // ===============================
                await _unitOfWork.BeginTransactionAsync();

                // ===============================
                // 5️⃣ FILE UPLOAD
                // ===============================
                string? docPath = null;
                string? docName = null;
                bool hasProofUploaded = false;

                if (request.DTO.ProofFile != null &&
                    request.DTO.ProofFile.Length > 0)
                {
                    try
                    {
                        string relation =
                            request.DTO.Relation.ToString() ?? "doc";

                        string fileName =
                            $"proof-{staffMemberId}-{relation}-{DateTime.UtcNow:yyyyMMddHHmmss}";

                        string folderPath =
                            $"{ConstantValues.InstitutionFolder}-{validation.InstitutionId}/" +
                            $"{ConstantValues.StaffMemberFolder}/{staffMemberId}/{ConstantValues.DependentFolder}";

                        uploadedFileKey = await _fileStorageService.UploadFileAsync(
                            request.DTO.ProofFile,
                            folderPath,
                            fileName);

                        if (!string.IsNullOrWhiteSpace(uploadedFileKey))
                        {
                            docPath = uploadedFileKey;
                            docName = fileName;
                            hasProofUploaded = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Dependent proof upload failed");
                        throw new ApiException("File upload failed.", 500);
                    }
                }

                // ===============================
                // 6️⃣ MAP ENTITY
                // ===============================
                var entity = _mapper.Map<StaffMemberDependent>(request.DTO);

                entity.StaffMemberId = staffMemberId;
                entity.AddedById = validation.LoggedInStaffMemberId;
                entity.AddedDateTime = DateTime.UtcNow;

                entity.IsActive = true;
                entity.IsEditAllowed = true;
                entity.IsInfoVerified = false;

                entity.FileType = hasProofUploaded ? 2 : 0;
                entity.FilePath = docPath;
                entity.FileName = docName;
                entity.HasProofUploaded = hasProofUploaded;
                entity.IsCoveredInPolicy =false; // Default, can be updated later by HR/Admin
                // ===============================
                // 7️⃣ SAVE
                // ===============================
                var responseDTO =
                    await _unitOfWork.StaffMemberDependentRepository
                        .CreateAsync(entity);

                if (responseDTO == null)
                    throw new ApiException("Failed to create dependent.", 500);

                // ===============================
                // 8️⃣ PROJECTION
                // ===============================
                var encryptedList =
                    ProjectionHelper.ToGetDependentResponseDTOs(
                        responseDTO.Data,
                        _idEncoderService,
                        validation.Claims.InstitutionEncriptionKey,
                        _configuration, _fileStorageService
                    );

                // ===============================
                // 9️⃣ COMMIT
                // ===============================
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("CreateDependent success");

                return ApiResponse<List<GetDependentResponseDTO>>
                    .Success(encryptedList, "Dependent created successfully.");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();

                _logger.LogError(ex, "Error creating dependent");

                //  FILE CLEANUP (CRITICAL )
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



