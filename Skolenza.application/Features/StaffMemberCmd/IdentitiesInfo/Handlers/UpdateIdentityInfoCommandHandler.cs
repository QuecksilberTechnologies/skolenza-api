// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the identity-update command and its legacy disabled flow.
// ================================================================

using skolenza.application.Common.Helpers.RequestHelper;
using skolenza.application.DTOS.Staff.Sensitive;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.IFileStorage;
using skolenza.application.Interfaces.IPermission;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.StaffMemberCmd.IdentitiesInfo.Handlers
{
    #region Command

    /// <summary>
    /// Represents a request to update staffMember identity information.
    /// </summary>
    public class UpdateIdentityInfoCommand : IRequest<ApiResponse<bool>>
    {
        public UpdateIdentityReqestDTO DTO { get; set; }

        public UpdateIdentityInfoCommand(UpdateIdentityReqestDTO dto)
        {
            DTO = dto;
        }
    }

    #endregion

    //public class UpdateIdentityInfoCommandHandler : IRequestHandler<UpdateIdentityInfoCommand, ApiResponse<bool>>
    //{
    //    private readonly IUnitOfWork _unitOfWork;
    //    private readonly ILogger<UpdateIdentityInfoCommandHandler> _logger;
    //    private readonly IPermissionService _permissionService;
    //    private readonly ICommonRequestService _commonRequestService;
    //    private readonly IIdEncoderService _idEncoderService;
    //    private readonly IFileStorageService _fileStorageService;

    //    public UpdateIdentityInfoCommandHandler(
    //        IUnitOfWork unitOfWork,
    //        ILogger<UpdateIdentityInfoCommandHandler> logger,
    //        IPermissionService permissionService,
    //        ICommonRequestService commonRequestService,
    //        IIdEncoderService idEncoderService,
    //        IFileStorageService fileStorageService)
    //    {
    //        _unitOfWork = unitOfWork;
    //        _logger = logger;
    //        _permissionService = permissionService;
    //        _commonRequestService = commonRequestService;
    //        _idEncoderService = idEncoderService;
    //        _fileStorageService = fileStorageService;
    //    }

        //public async Task<ApiResponse<bool>> Handle(UpdateIdentityInfoCommand request, CancellationToken cancellationToken)
        //{
        //    await _unitOfWork.BeginTransactionAsync();

        //    try
        //    {
        //        // =================================================
        //        // 1️⃣ VALIDATION
        //        // =================================================
        //        var validation = await _commonRequestService
        //            .ValidateInstitutionUserRequestAsync();

        //        if (!validation.Success)
        //            return ApiResponse<bool>.Fail(validation.ErrorMessage);

        //        // =================================================
        //        // 2️⃣ FETCH EXISTING RECORD
        //        // =================================================
        //        var emp = await _unitOfWork.StaffMemberIdentityRepository
        //            .GetSingleRecordAsync(request.DTO.Id, true);

        //        if (emp == null)
        //            return ApiResponse<bool>.Fail("Identity record not found.");

        //        var dto = request.DTO;

        //        // =================================================
        //        // 3️⃣ UPDATE BASIC FIELDS
        //        // =================================================
        //        if (!string.IsNullOrWhiteSpace(dto.AadhaarNumber))
        //            emp.AadhaarNumber = dto.AadhaarNumber.Trim();

        //        if (!string.IsNullOrWhiteSpace(dto.PanNumber))
        //            emp.PanNumber = dto.PanNumber.Trim();

        //        if (!string.IsNullOrWhiteSpace(dto.PassportNumber))
        //            emp.PassportNumber = dto.PassportNumber.Trim();

        //        if (!string.IsNullOrWhiteSpace(dto.DrivingLicenseNumber))
        //            emp.DrivingLicenseNumber = dto.DrivingLicenseNumber.Trim();

        //        if (!string.IsNullOrWhiteSpace(dto.VoterId))
        //            emp.VoterId = dto.VoterId.Trim();

        //        if (!string.IsNullOrWhiteSpace(dto.BloodGroup))
        //            emp.BloodGroup = dto.BloodGroup.Trim();

        //        if (!string.IsNullOrWhiteSpace(dto.Nationality))
        //            emp.Nationality = dto.Nationality.Trim();

        //        if (!string.IsNullOrWhiteSpace(dto.EmergencyContactName))
        //            emp.EmergencyContactName = dto.EmergencyContactName.Trim();

        //        if (!string.IsNullOrWhiteSpace(dto.EmergencyContactRelation))
        //            emp.EmergencyContactRelation = dto.EmergencyContactRelation.Trim();

        //        if (!string.IsNullOrWhiteSpace(dto.EmergencyContactNumber))
        //            emp.EmergencyContactNumber = dto.EmergencyContactNumber.Trim();

        //        if (dto.MaritalStatus.HasValue)
        //            emp.MaritalStatus = dto.MaritalStatus.Value;

        //        if (dto.HasEPFAccount != emp.HasEPFAccount)
        //            emp.HasEPFAccount = dto.HasEPFAccount;

        //        if (!string.IsNullOrWhiteSpace(dto.UANNumber))
        //            emp.UANNumber = dto.UANNumber.Trim();

        //        // =================================================
        //        // 4️⃣ FILE UPLOAD (AADHAAR – S3 NO DELETE)
        //        // =================================================
        //        if (dto.AadhaarDocFile != null && dto.AadhaarDocFile.Length > 0)
        //        {
        //            string masked =
        //                !string.IsNullOrWhiteSpace(emp.AadhaarNumber) &&
        //                emp.AadhaarNumber.Length > 4
        //                    ? emp.AadhaarNumber[^4..]
        //                    : "XXXX";

        //            string fileName =
        //                $"id-aadhaar-{emp.StaffMemberId}-{masked}-{DateTime.UtcNow:yyyyMMddHHmmss}";

        //            string folderPath =
        //                $"institution-{validation.InstitutionId}/staffMembers/{emp.StaffMemberId}/identity";

        //            var fileKey = await _fileStorageService.UploadFileAsync(
        //                dto.AadhaarDocFile,
        //                folderPath,
        //                fileName);

        //            emp.AadhaarDocPath = fileKey;
        //            emp.AadhaarDocName = fileName;
        //            emp.HasAadhaarIdUploaded = true;
        //        }

        //        // =================================================
        //        // 5️⃣ SAVE
        //        // =================================================
        //        emp.UpdatedById = validation.UserStaffMemberId;
        //        emp.UpdatedDateTime = DateTime.UtcNow;

        //        var isUpdated = await _unitOfWork.StaffMemberIdentityRepository.UpdateIdentity(emp);

        //        if (!isUpdated)
        //        {
        //            await _unitOfWork.RollbackTransactionAsync();
        //            return ApiResponse<bool>.Fail("Identity update failed.");
        //        }

        //        await _unitOfWork.CommitTransactionAsync();
        //        return ApiResponse<bool>.Success(true, "Identity updated successfully.");
        //    }
        //    catch (Exception ex)
        //    {
        //        await _unitOfWork.RollbackTransactionAsync();

        //        _logger.LogError(ex, "Error updating identity");

        //        return ApiResponse<bool>.Fail("Unexpected error", new List<string> { ex.Message });
        //    }
        //}
    
    
    //}


}
