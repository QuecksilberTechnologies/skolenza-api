// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Handles UpdateBankCommandHandler requests using authenticated institution context.
// ================================================================

using AutoMapper;
using skolenza.application.Common.Helpers;
using skolenza.application.Common.Helpers.RequestHelper;
using skolenza.application.Constants;
using skolenza.application.DTOS.Staff.Bank;
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

namespace skolenza.application.Features.StaffMemberCmd.BankInfo.Handlers
{

    #region Command

    /// <summary>
    /// Represents the UpdateBankCommand application component.
    /// </summary>
    public class UpdateBankCommand : IRequest<ApiResponse<bool>>
    {
        public UpdateBankReqestDTO DTO { get; set; }

        public UpdateBankCommand(UpdateBankReqestDTO dto)
        {
            DTO = dto;
        }

    }
    /// <summary>
    /// Handles authenticated institution requests for this feature.
    /// </summary>
        #endregion

    #region Handler

public class UpdateBankCommandHandler : IRequestHandler<UpdateBankCommand, ApiResponse<bool>>
    {

        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UpdateBankCommandHandler> _logger;
        private readonly IMapper _mapper;
        private readonly ITokenService _tokenService;
        private readonly IPermissionService _permissionService;
        private readonly IConfiguration _config;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IEncryptionService _encryptionService;
        private readonly ICommonRequestService _commonRequestService;
        private readonly IFileStorageService _fileStorageService;
        private readonly IIdEncoderService _idEncoderService;
        public UpdateBankCommandHandler(
            IBaseStaffMemberRepository staffMemberRepository,
            IUnitOfWork unitOfWork,
            ILogger<UpdateBankCommandHandler> logger,
            IMapper mapper,
            ITokenService tokenService,
            IPermissionService permissionRepository,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor, IEncryptionService encryptionService ,  ICommonRequestService commonRequestService, IFileStorageService fileStorageService,
             IIdEncoderService idEncoderService
            )
        {

            _unitOfWork = unitOfWork;
            _logger = logger;
            _mapper = mapper;
            _tokenService = tokenService;
            _permissionService = permissionRepository;
            _config = configuration;
            _httpContextAccessor = httpContextAccessor;
            _encryptionService = encryptionService;
            _commonRequestService = commonRequestService;
            _fileStorageService = fileStorageService;
            _idEncoderService = idEncoderService;

        }

        public async Task<ApiResponse<bool>> Handle(
      UpdateBankCommand request,
      CancellationToken cancellationToken)
        {
            string? uploadedFileKey = null;

            try
            {
                _logger.LogInformation("UpdateBank started");

                // ===============================
                // 1️⃣ VALIDATION
                // ===============================
                #region Institution Request Validation
                var validation = await _commonRequestService
                    .ValidateInstitutionUserRequestAsync();
                #endregion

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
                // 4️⃣ FETCH EXISTING
                // ===============================
                var bank = await _unitOfWork.StaffMemberBankRepository
                    .GetSingleRecordAsync(request.DTO.Id, true);

                if (bank == null)
                    throw new ApiException("Bank record not found.", 404);

                if (bank.StaffMemberId != staffMemberId ||
                    !await _commonRequestService.CanAccessStaffMemberDataAsync(
                        validation, bank.StaffMemberId, StaffMemberDataAccessRequirement.PersonalDetails, cancellationToken))
                {
                    throw new ForbiddenAccessException(AppConstants.ErrorMessages.PermissionDenied);
                }

                if (validation.RoleTypeId != ConstantValues.RoleTypeAdmin &&
                    (bank.IsInfoVerified || !bank.IsEditAllowed))
                {
                    throw new ForbiddenAccessException(AppConstants.ErrorMessages.PermissionDenied);
                }

                var dto = request.DTO;

                // ===============================
                // 5️⃣ UPDATE FIELDS
                // ===============================
                if (!string.IsNullOrWhiteSpace(dto.BankName))
                    bank.BankName = dto.BankName.Trim();

                if (!string.IsNullOrWhiteSpace(dto.AccountNumber))
                    bank.AccountNumber = _encryptionService.Encrypt(
                        dto.AccountNumber.Trim(),
                        validation.Claims.InstitutionEncriptionKey);

                if (!string.IsNullOrWhiteSpace(dto.IFSCCode))
                    bank.Ifsccode = _encryptionService.Encrypt(
                        dto.IFSCCode.Trim().ToUpperInvariant(),
                        validation.Claims.InstitutionEncriptionKey);

                if (!string.IsNullOrWhiteSpace(dto.BranchName))
                    bank.BranchName = dto.BranchName.Trim();

                if (!string.IsNullOrWhiteSpace(dto.AccountType))
                    bank.AccountType = AccountTypeHelper.Normalize(dto.AccountType);

                if (!string.IsNullOrWhiteSpace(dto.UPIId))
                    bank.Upiid = _encryptionService.Encrypt(
                        dto.UPIId.Trim(),
                        validation.Claims.InstitutionEncriptionKey);

                // ===============================
                // 6️⃣ PRIMARY ACCOUNT RULE
                // ===============================
                if (dto.IsPrimaryAccount)
                {
                    if (dto.CancelledChequeFile == null && !bank.HasChequeDocUploaded)
                        throw new ValidationErrorException("Cancelled cheque is mandatory.");

                    var resetDone =
                        await _unitOfWork.StaffMemberBankRepository
                            .ResetPrimaryAccountAsync(
                                staffMemberId,
                                validation.LoggedInStaffMemberId);

                    if (!resetDone)
                        throw new ApiException("Failed to reset primary accounts.", 500);

                    bank.IsPrimaryAccount = true;
                }
                else
                {
                    bank.IsPrimaryAccount = false;
                }

                // ===============================
                // 7️⃣ START TRANSACTION
                // ===============================
                await _unitOfWork.BeginTransactionAsync();

                // ===============================
                // 8️⃣ FILE UPLOAD
                // ===============================
                if (dto.CancelledChequeFile != null &&
                    dto.CancelledChequeFile.Length > 0)
                {
                    try
                    {
                        string folderPath =
                            $"{ConstantValues.InstitutionFolder}-{validation.InstitutionId}/" +
                            $"{ConstantValues.StaffMemberFolder}/{staffMemberId}/" +
                            $"{ConstantValues.BankFolder}";

                        string newFileName =
                            $"{ConstantValues.BankFolder}-{staffMemberId}-{DateTime.UtcNow:yyyyMMddHHmmss}";

                        uploadedFileKey = await _fileStorageService.UploadFileAsync(
                            dto.CancelledChequeFile,
                            folderPath,
                            newFileName);

                        bank.FilePath = uploadedFileKey;
                        bank.FileName = newFileName;
                        bank.HasChequeDocUploaded = true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Cheque upload failed");
                        throw new ApiException("File upload failed.", 500);
                    }
                }

                bank.UpdatedDateTime = DateTime.UtcNow;
                bank.UpdatedById = validation.LoggedInStaffMemberId;

                bool isSuccess =
                    await _unitOfWork.StaffMemberBankRepository.UpdateAsync(bank);

                if (!isSuccess)
                    throw new ApiException("Failed to update bank info.", 500);

                // ===============================
                //  COMMIT
                // ===============================
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("UpdateBank success");

                return ApiResponse<bool>.Success(true, "Bank updated successfully.");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();

                _logger.LogError(ex, "Error updating bank info");

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
