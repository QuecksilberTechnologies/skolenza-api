// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Handles CreateStaffMemberIdentityCommandHandler requests using authenticated institution context.
// ================================================================

using skolenza.application.Common.Helpers.RequestHelper;
using skolenza.application.Constants;
using skolenza.application.DTOs.Module;
using skolenza.application.DTOS.Staff.Sensitive;
using skolenza.application.Exceptions;
using skolenza.application.Features.StaffMemberCmd.StaffMemberBase.Handlers;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.IFileStorage;
using skolenza.application.Interfaces.IPermission;
using skolenza.application.Wrappers;

using skolenza.domain.Entity;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace skolenza.application.Features.StaffMemberCmd.IdentitiesInfo.Handlers
{
    public class CreateIdentityInfoCommand : IRequest<ApiResponse<bool>>
    {
        public CreateStaffMemberIdentityRequestDTO DTO { get; set; }

        public CreateIdentityInfoCommand(CreateStaffMemberIdentityRequestDTO dto)
        {
            DTO = dto;
        }
    }

    /// <summary>
    /// Handles authenticated institution requests for this feature.
    /// </summary>
    public class CreateStaffMemberIdentityCommandHandler  : IRequestHandler<CreateIdentityInfoCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICommonRequestService _commonRequestService;
        private readonly IIdEncoderService _idEncoderService;
        private readonly IFileStorageService _fileStorageService;
        private readonly ILogger<CreateIdentityInfoCommand> _logger;
        public CreateStaffMemberIdentityCommandHandler(
            IUnitOfWork unitOfWork,
            ICommonRequestService commonRequestService,
            IIdEncoderService idEncoderService,
            IFileStorageService fileStorageService, ILogger<CreateIdentityInfoCommand> logger)
        {
            _unitOfWork = unitOfWork;
            _commonRequestService = commonRequestService;
            _idEncoderService = idEncoderService;
            _fileStorageService = fileStorageService;
            _logger = logger;   
        }

        public async Task<ApiResponse<bool>> Handle(
    CreateIdentityInfoCommand request,
    CancellationToken cancellationToken)
        {
            var uploadedFiles = new List<string>();
            var replacedFiles = new List<string>();

            try
            {
                _logger.LogInformation("CreateStaffMemberIdentity started");

                // ===============================
                // 1️⃣ VALIDATION
                // ===============================
                if (request?.DTO?.Identities == null || !request.DTO.Identities.Any())
                    throw new ValidationErrorException("No identity data received.");

                var first = request.DTO.Identities.First();

                #region Institution Request Validation
                var validation =
                    await _commonRequestService.ValidateInstitutionUserRequestAsync();
                #endregion

                if (!validation.Success)
                    throw new UnauthorizedAccessException(validation.ErrorMessage);
                // 3️⃣ START TRANSACTION
                // ===============================
                await _unitOfWork.BeginTransactionAsync();

                var entities = new List<StaffMemberIdentity>();
                var requestDocuments = new HashSet<(long StaffMemberId, int DocumentId)>();

                foreach (var identity in request.DTO.Identities)
                {
                    var staffMemberId =
                        RequestCommonHelper.DecodeOnlyStaffMemberId(
                            identity.StaffMemberId,
                            validation.Claims.InstitutionEncriptionKey,
                            _idEncoderService);

                    if (staffMemberId <= 0)
                        throw new ValidationErrorException("Invalid StaffMemberId.");

                    if (!requestDocuments.Add((staffMemberId, identity.IdentityCategoryDocumentId)))
                        throw new ValidationErrorException("Duplicate identity document received.");

                    if (!await _commonRequestService.CanAccessStaffMemberDataAsync(
                            validation,
                            staffMemberId,
                            StaffMemberDataAccessRequirement.PersonalDetails,
                            cancellationToken))
                    {
                        throw new ForbiddenAccessException(AppConstants.ErrorMessages.PermissionDenied);
                    }

                    if (string.IsNullOrWhiteSpace(identity.IdentityValue) || identity.IdentityValue.Length > 100)
                    {
                        throw new ValidationErrorException("Identity value must contain between 1 and 100 characters.");
                    }

                    if (!await _unitOfWork.StaffMemberIdentityRepository.IsDocumentAllowedAsync(
                            staffMemberId,
                            validation.InstitutionId,
                            identity.IdentityCategoryDocumentId,
                            cancellationToken))
                    {
                        throw new ValidationErrorException("Identity document is not configured for this staffMember's country.");
                    }

                    var effectiveFrom = identity.EffectiveFrom ?? DateOnly.FromDateTime(DateTime.UtcNow);
                    if (identity.EffectiveTo.HasValue && identity.EffectiveTo.Value < effectiveFrom)
                    {
                        throw new ValidationErrorException("Identity expiry cannot precede its effective date.");
                    }

                    string? documentPath = null;
                    string? documentName = null;

                    // ===============================
                    // 4️⃣ FILE UPLOAD
                    // ===============================
                    if (identity.IdentityDocumentFile != null &&
                        identity.IdentityDocumentFile.Length > 0)
                    {
                        try
                        {
                            string maskedValue =
                                identity.IdentityValue?.Length > 4
                                    ? identity.IdentityValue[^4..]
                                    : "XXXX";

                            string docCode =
                                identity.DocumnetCode?.Trim().ToLower().Replace(" ", "_") ?? "id";

                            documentName =
                                $"id-{docCode}-{staffMemberId}-{maskedValue}-{DateTime.UtcNow:yyyyMMddHHmmss}";

                            string folderPath =
                                $"{ConstantValues.InstitutionFolder}-{validation.InstitutionId}/" +
                                $"{ConstantValues.StaffMemberFolder}/{staffMemberId}/identity";

                            var fileKey =
                                await _fileStorageService.UploadFileAsync(
                                    identity.IdentityDocumentFile,
                                    folderPath,
                                    documentName);

                            if (!string.IsNullOrWhiteSpace(fileKey))
                            {
                                documentPath = fileKey;
                                uploadedFiles.Add(fileKey); // 🔥 TRACK FILE
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Identity file upload failed");
                            throw new ApiException("Identity file upload failed.", 500);
                        }
                    }

                    // ===============================
                    // 5️⃣ BUILD ENTITY
                    // ===============================
                    var existing = await _unitOfWork.StaffMemberIdentityRepository
                        .GetByStaffMemberAndDocumentAsync(
                            staffMemberId,
                            identity.IdentityCategoryDocumentId,
                            cancellationToken);

                    if (existing != null)
                    {
                        if (validation.RoleTypeId != ConstantValues.RoleTypeAdmin &&
                            (existing.IsInfoVerified || !existing.IsEditAllowed))
                            throw new ForbiddenAccessException(AppConstants.ErrorMessages.PermissionDenied);

                        if (!string.IsNullOrWhiteSpace(documentPath) &&
                            !string.IsNullOrWhiteSpace(existing.DocumentFilePath))
                            replacedFiles.Add(existing.DocumentFilePath);

                        existing.IdentityValue = identity.IdentityValue.Trim();
                        existing.EffectiveFrom = effectiveFrom;
                        existing.EffectiveTo = identity.EffectiveTo;
                        existing.DocumentFileName = documentName ?? existing.DocumentFileName;
                        existing.DocumentFilePath = documentPath ?? existing.DocumentFilePath;
                        existing.HasIdentityUploaded = !string.IsNullOrWhiteSpace(existing.DocumentFilePath);
                        existing.UpdatedById = validation.UserStaffMemberId;
                        existing.UpdatedDateTime = DateTime.UtcNow;

                        if (!await _unitOfWork.StaffMemberIdentityRepository.UpdateAsync(
                                existing,
                                cancellationToken))
                            throw new ApiException("Identity update failed.", 500);

                        continue;
                    }

                    entities.Add(new StaffMemberIdentity
                    {
                        StaffMemberId = staffMemberId,
                        IdentityCategoryDocumentId = identity.IdentityCategoryDocumentId,
                        IdentityValue = identity.IdentityValue,

                        DocumentFileName = documentName,
                        DocumentFilePath = documentPath,

                        EffectiveFrom = effectiveFrom,
                        EffectiveTo = identity.EffectiveTo,

                        HasIdentityUploaded = documentPath != null,
                        IsEditAllowed = true,
                        IsActive = true,

                        AddedById = validation.UserStaffMemberId,
                        AddedDateTime = DateTime.UtcNow
                    });
                }

                // ===============================
                // 6️⃣ SAVE
                // ===============================
                var isSuccess = entities.Count == 0 ||
                    await _unitOfWork.StaffMemberIdentityRepository.CreateAsync(entities);

                if (!isSuccess)
                    throw new ApiException("Identity save failed.", 500);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // ===============================
                // 7️⃣ COMMIT
                // ===============================
                await _unitOfWork.CommitTransactionAsync();

                foreach (var replacedFile in replacedFiles)
                {
                    try
                    {
                        await _fileStorageService.DeleteFileAsync(replacedFile);
                    }
                    catch (Exception cleanupEx)
                    {
                        _logger.LogWarning(cleanupEx, "Failed to delete replaced identity file");
                    }
                }

                _logger.LogInformation("CreateStaffMemberIdentity success");

                return ApiResponse<bool>.Success(
                    true,
                    "Identity details saved successfully.");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();

                _logger.LogError(ex, "CreateStaffMemberIdentity failed");

                // 🔥 FILE CLEANUP (CRITICAL)
                foreach (var file in uploadedFiles)
                {
                    try
                    {
                        await _fileStorageService.DeleteFileAsync(file);
                    }
                    catch (Exception cleanupEx)
                    {
                        _logger.LogError(cleanupEx, "Failed to cleanup identity file");
                    }
                }

                throw; // 🚨 MUST
            }
        }


    }



}
