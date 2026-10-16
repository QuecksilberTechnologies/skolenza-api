// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles Update Base StaffMember By Admin Command Handler requests.
// ================================================================

using skolenza.application.Common.Helpers.RequestHelper;
using skolenza.application.Constants;
using skolenza.application.DTOS.Staff.BaseStaffMember;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.IPermission;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.StaffMemberCmd.StaffMemberBase.Handlers
{

    #region Command



    /// <summary>
    /// Represents the UpdateBaseStaffMemberByAdminCommand application component.
    /// </summary>
    public class UpdateBaseStaffMemberByAdminCommand
       : IRequest<ApiResponse<bool>>
    {
        public UpdateStaffMemberRequestOfficialDTO DTO { get; }

        public UpdateBaseStaffMemberByAdminCommand(UpdateStaffMemberRequestOfficialDTO dto)
        {
            DTO = dto;
        }
    }

    /// <summary>
    /// Handles UpdateBaseStaffMemberByAdminCommand requests.
    /// </summary>
        #endregion

    #region Handler

public class UpdateBaseStaffMemberByAdminCommandHandler
        : IRequestHandler<UpdateBaseStaffMemberByAdminCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UpdateBaseStaffMemberByAdminCommandHandler> _logger;
        private readonly IIdEncoderService _idEncoderService;
        private readonly ICommonRequestService _commonRequestService;
        private readonly IPermissionService _permissionService;

        public UpdateBaseStaffMemberByAdminCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<UpdateBaseStaffMemberByAdminCommandHandler> logger,
            IIdEncoderService idEncoderService,
            ICommonRequestService commonRequestService,
            IPermissionService permissionService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _idEncoderService = idEncoderService;
            _commonRequestService = commonRequestService;
            _permissionService = permissionService;
        }

        public async Task<ApiResponse<bool>> Handle(
      UpdateBaseStaffMemberByAdminCommand request,
      CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("UpdateBaseStaffMemberByAdmin started");

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
                // 4️⃣ FETCH EXISTING
                // ===============================
                var staffMember =
                    await _unitOfWork.StaffMembers.GetByIdAsync(
                        staffMemberId,
                        validation.InstitutionId,
                        true);

                if (staffMember == null)
                    throw new ApiException("StaffMember not found.", 404);

                // ===============================
                // 5️⃣ START TRANSACTION
                // ===============================
                await _unitOfWork.BeginTransactionAsync();

                // ===============================
                // 6️⃣ PATCH UPDATE
                // ===============================
                if (!string.IsNullOrWhiteSpace(request.DTO.FirstName))
                    staffMember.FirstName = request.DTO.FirstName.Trim();

                if (!string.IsNullOrWhiteSpace(request.DTO.MiddleName))
                    staffMember.MiddleName = request.DTO.MiddleName.Trim();

                if (!string.IsNullOrWhiteSpace(request.DTO.LastName))
                    staffMember.LastName = request.DTO.LastName.Trim();

                if (!string.IsNullOrWhiteSpace(request.DTO.Description))
                    staffMember.Description = request.DTO.Description.Trim();

                if (!string.IsNullOrWhiteSpace(request.DTO.Remark))
                    staffMember.Remark = request.DTO.Remark.Trim();



                if (request.DTO.DepartmentId.HasValue)
                    staffMember.DepartmentId = request.DTO.DepartmentId.Value;

                if (request.DTO.StaffEmploymentTypeId.HasValue)
                {
                    var staffEmploymentType = await _unitOfWork.StaffEmploymentTypeRepository.GetStaffEmploymentTypeByIdAsync(
                        validation.InstitutionId, request.DTO.StaffEmploymentTypeId.Value, cancellationToken);
                    if (staffEmploymentType?.IsActive != true)
                    {
                        throw new ValidationErrorException("StaffEmploymentType must be active and belong to this institution.");
                    }
                    staffMember.StaffEmploymentTypeId = request.DTO.StaffEmploymentTypeId.Value;
                }

                if (request.DTO.InstitutionStaffCategoryMasterId.HasValue)
                {
                    if (!request.DTO.StaffPositionMasterId.HasValue || request.DTO.StaffPositionMasterId.Value <= 0)
                    {
                        throw new ValidationErrorException("A Position is required when Staff Category changes.");
                    }
                    var institutionCategory = (await _unitOfWork.StaffProfessionalStructureRepository
                        .GetInstitutionCategoriesAsync(validation.InstitutionId, true, cancellationToken))
                        .SingleOrDefault(item => item.Id == request.DTO.InstitutionStaffCategoryMasterId.Value)
                        ?? throw new ValidationErrorException("Staff Category must be active and belong to this institution.");
                    staffMember.InstitutionStaffCategoryMasterId = institutionCategory.Id;
                    staffMember.StaffCategoryId = institutionCategory.StaffCategoryTypeId;
                }

                if (request.DTO.CountryId.HasValue)
                    staffMember.CountryId = request.DTO.CountryId.Value;

                if (request.DTO.GenderId.HasValue)
                    staffMember.GenderId = request.DTO.GenderId.Value;

                if (request.DTO.DateOfBirth.HasValue)
                    staffMember.DateOfBirth = request.DTO.DateOfBirth.Value;

                if (request.DTO.DateOfOnBoarding.HasValue)
                    staffMember.DateOfOnBoarding = request.DTO.DateOfOnBoarding.Value;

                if (request.DTO.DateOfExit.HasValue)
                    staffMember.DateOfExit = request.DTO.DateOfExit.Value;

                if (request.DTO.HasPermanent.HasValue)
                    staffMember.HasPermanent = request.DTO.HasPermanent.Value;

                if (request.DTO.IsActive.HasValue)
                    staffMember.IsActive = request.DTO.IsActive.Value;

                if (request.DTO.IsEditAllowed.HasValue)
                    staffMember.IsEditAllowed = request.DTO.IsEditAllowed.Value;

                if (request.DTO.Relation.HasValue)
                    staffMember.Relation = request.DTO.Relation.Value;

                if (!string.IsNullOrWhiteSpace(request.DTO.EmergencyContactPerson))
                    staffMember.EmergencyContactPerson =
                        request.DTO.EmergencyContactPerson;

                if (!string.IsNullOrWhiteSpace(request.DTO.EmergencyContactNumber))
                    staffMember.EmergencyContactNumber =
                        request.DTO.EmergencyContactNumber;

                if (!string.IsNullOrWhiteSpace(request.DTO.MobileNumber))
                    staffMember.MobileNumber = request.DTO.MobileNumber;

                if (!string.IsNullOrWhiteSpace(request.DTO.BloodGroup))
                    staffMember.BloodGroup = request.DTO.BloodGroup;

                if (request.DTO.IsMarried.HasValue)
                    staffMember.IsMarried = request.DTO.IsMarried.Value;

                // ===============================
                // INFO VERIFIED
                // ===============================
                if (request.DTO.IsInfoVerified.HasValue)
                {
                    staffMember.IsInfoVerified =
                        request.DTO.IsInfoVerified.Value;

                    if (request.DTO.IsInfoVerified.Value)
                    {
                        staffMember.InfoVerifiedById =
                            validation.UserStaffMemberId;
                        staffMember.InfoVerifiedDateTime =
                            DateTime.UtcNow;
                    }
                    else
                    {
                        staffMember.InfoVerifiedById = null;
                        staffMember.InfoVerifiedDateTime = null;
                    }
                }

                // ===============================
                // 7️⃣ AUDIT
                // ===============================
                staffMember.UpdatedById = validation.UserStaffMemberId;
                staffMember.UpdatedDateTime = DateTime.UtcNow;

                // ===============================
                // 8️⃣ SAVE
                // ===============================
                await _unitOfWork.StaffMembers.UpdateStaffMemberAsync(
                    staffMember,
                    validation.InstitutionId);

                if (request.DTO.StaffPositionMasterId.HasValue)
                {
                    if (request.DTO.StaffPositionMasterId.Value <= 0)
                    {
                        throw new ValidationErrorException("StaffPositionMasterId must be greater than zero.");
                    }

                    await _unitOfWork.StaffProfessionalStructureRepository.ReplacePrimaryPositionAsync(
                        validation.InstitutionId,
                        staffMemberId,
                        request.DTO.StaffPositionMasterId.Value,
                        DateOnly.FromDateTime(staffMember.DateOfOnBoarding ?? DateTime.UtcNow),
                        validation.UserStaffMemberId,
                        cancellationToken);
                }

                // ===============================
                // 9️⃣ COMMIT
                // ===============================
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("UpdateBaseStaffMemberByAdmin success");

                return ApiResponse<bool>.Success(
                    true,
                    "StaffMember updated successfully.");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();

                _logger.LogError(ex, "Error updating staffMember");

                throw; //  MUST
            }
        }
    }


    #endregion
}


