// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles Update Experience Info Command Handler requests.
// ================================================================

using AutoMapper;
using skolenza.application.Common.Helpers.RequestHelper;
using skolenza.application.DTOS.Staff.Experience;
using skolenza.application.Exceptions;
using skolenza.application.Constants;
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

namespace skolenza.application.Features.StaffMemberCmd.ExperienceInfo.Handlers
{

    #region Command

    /// <summary>
    /// Represents the UpdateExperienceInfoCommand application component.
    /// </summary>
    public class UpdateExperienceInfoCommand : IRequest<ApiResponse<bool>>
    {
        public UpdateExperienceRequestDTO DTO { get; set; }

        public UpdateExperienceInfoCommand(UpdateExperienceRequestDTO dto)
        {
            DTO = dto;
        }

    }
    /// <summary>
    /// Handles UpdateExperienceInfoCommand requests.
    /// </summary>
        #endregion

    #region Handler

public class UpdateExperienceInfoCommandHandler
       : IRequestHandler<UpdateExperienceInfoCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UpdateExperienceInfoCommand> _logger;
        private readonly IIdEncoderService _idEncoderService;
        private readonly ICommonRequestService _commonRequestService;

        public UpdateExperienceInfoCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<UpdateExperienceInfoCommand> logger,
            IIdEncoderService idEncoderService,
            ICommonRequestService commonRequestService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _idEncoderService = idEncoderService;
            _commonRequestService = commonRequestService;
        }

        public async Task<ApiResponse<bool>> Handle(
            UpdateExperienceInfoCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("🚀 UpdateExperience started");

                // ===============================
                // 1️⃣ VALIDATION
                // ===============================

                var validation = await _commonRequestService.ValidateInstitutionUserRequestAsync();

                if (!validation.Success)
                    throw new UnauthorizedAccessException(validation.ErrorMessage);

                if (request?.DTO == null)
                    throw new ValidationErrorException("Invalid request");

                if (request.DTO.Id < 0)
                    throw new ValidationErrorException("ExperienceId is required");
                var staffMemberId = RequestCommonHelper.DecodeOnlyStaffMemberId(
                    request.DTO.StaffMemberId,
                    validation.Claims.InstitutionEncriptionKey,
                    _idEncoderService);


                // ===============================
                // 3️⃣ FETCH EXISTING (SINGLE)
                // ===============================
                var existing = await _unitOfWork.StaffMemberExperienceRepository
                    .GetByIdAsync(request.DTO.Id, staffMemberId);

                if (existing == null)
                    throw new ApiException("Experience not found", 404);

                if (!await _commonRequestService.CanAccessStaffMemberDataAsync(
                        validation, existing.StaffMemberId, StaffMemberDataAccessRequirement.PersonalDetails, cancellationToken))
                    throw new ForbiddenAccessException(AppConstants.ErrorMessages.PermissionDenied);

                if (validation.RoleTypeId != ConstantValues.RoleTypeAdmin &&
                    (existing.IsInfoVerified == true || existing.IsEditAllowed != true))
                    throw new ForbiddenAccessException(AppConstants.ErrorMessages.PermissionDenied);

                skolenza.application.Common.Helpers.StaffMemberProfileValidationHelper.RequireText(
                    string.IsNullOrWhiteSpace(request.DTO.CompanyName)
                        ? existing.CompanyName
                        : request.DTO.CompanyName,
                    "Company name");
                skolenza.application.Common.Helpers.StaffMemberProfileValidationHelper.RequireText(
                    string.IsNullOrWhiteSpace(request.DTO.Designation)
                        ? existing.Designation
                        : request.DTO.Designation,
                    "Designation");
                skolenza.application.Common.Helpers.StaffMemberProfileValidationHelper.ValidateDateRange(
                    request.DTO.StartDate ?? existing.StartDate,
                    request.DTO.EndDate ?? existing.EndDate);

                if (request.DTO.IsAnyGap)
                {
                    skolenza.application.Common.Helpers.StaffMemberProfileValidationHelper.RequireText(
                        request.DTO.ReasonOfGap,
                        "Gap reason");
                    skolenza.application.Common.Helpers.StaffMemberProfileValidationHelper.ValidateDateRange(
                        request.DTO.GapYearFrom,
                        request.DTO.GapYearTo,
                        "Gap start date",
                        "Gap end date");
                }

                // ===============================
                // 4️⃣ START TRANSACTION
                // ===============================
                await _unitOfWork.BeginTransactionAsync();

                // ===============================
                // 5️⃣ UPDATE (SAFE PARTIAL)
                // ===============================

                if (!string.IsNullOrWhiteSpace(request.DTO.CompanyName))
                    existing.CompanyName = request.DTO.CompanyName;

                if (!string.IsNullOrWhiteSpace(request.DTO.Designation))
                    existing.Designation = request.DTO.Designation;

                if (!string.IsNullOrWhiteSpace(request.DTO.StaffMemberIdOfCompany))
                    existing.StaffMemberIdOfCompany = request.DTO.StaffMemberIdOfCompany;

                if (request.DTO.Ctc.HasValue)
                    existing.Ctc = request.DTO.Ctc;

                if (request.DTO.StartDate.HasValue)
                    existing.StartDate = request.DTO.StartDate;

                if (request.DTO.EndDate.HasValue)
                    existing.EndDate = request.DTO.EndDate;

                if (request.DTO.Experience.HasValue)
                    existing.Experience = request.DTO.Experience;

                if (request.DTO.IsWFH)
                    existing.IsWfh = request.DTO.IsWFH;

                if (request.DTO.WorkingCountryId.HasValue)
                    existing.WorkingCountryId = request.DTO.WorkingCountryId;

                if (request.DTO.WorkingStateId.HasValue)
                    existing.WorkingStateId = request.DTO.WorkingStateId;

                if (request.DTO.WorkingDistrictId.HasValue)
                    existing.WorkingDistrictId = request.DTO.WorkingDistrictId;

                if (request.DTO.IsForeignExperience)
                    existing.IsForeignExperience = request.DTO.IsForeignExperience;

                if (!string.IsNullOrWhiteSpace(request.DTO.ReasonForLeaving))
                    existing.ReasonForLeaving = request.DTO.ReasonForLeaving;

                if (!string.IsNullOrWhiteSpace(request.DTO.Remark))
                    existing.Remark = request.DTO.Remark;

                if (!string.IsNullOrWhiteSpace(request.DTO.ColleagueName))
                    existing.ColleagueName = request.DTO.ColleagueName;

                if (!string.IsNullOrWhiteSpace(request.DTO.ColleagueDesignation))
                    existing.ColleagueDesignation = request.DTO.ColleagueDesignation;

                if (!string.IsNullOrWhiteSpace(request.DTO.ColleagueContactNumber))
                    existing.ColleagueContactNumber = request.DTO.ColleagueContactNumber;

                if (!string.IsNullOrWhiteSpace(request.DTO.ReportingManagerName))
                    existing.ReportingManagerName = request.DTO.ReportingManagerName;

                if (!string.IsNullOrWhiteSpace(request.DTO.ReportingManagerNumber))
                    existing.ReportingManagerNumber = request.DTO.ReportingManagerNumber;

                if (!string.IsNullOrWhiteSpace(request.DTO.VerificationEmail))
                    existing.VerificationEmail = request.DTO.VerificationEmail;

                if (request.DTO.IsAnyGap)
                    existing.IsAnyGap = request.DTO.IsAnyGap;

                if (!string.IsNullOrWhiteSpace(request.DTO.ReasonOfGap))
                    existing.ReasonOfGap = request.DTO.ReasonOfGap;

                if (request.DTO.GapYearFrom.HasValue)
                    existing.GapYearFrom = request.DTO.GapYearFrom;

                if (request.DTO.GapYearTo.HasValue)
                    existing.GapYearTo = request.DTO.GapYearTo;

                //  Audit
                existing.UpdatedById = validation.UserStaffMemberId;
                existing.UpdatedDateTime = DateTime.UtcNow;

                // ===============================
                // 6️⃣ SAVE
                // ===============================
                await _unitOfWork.StaffMemberExperienceRepository.UpdateAsync(existing);
                await _unitOfWork.SaveChangesAsync();

                // ===============================
                // 7️⃣ COMMIT
                // ===============================
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("✅ Experience updated successfully");

                return ApiResponse<bool>.Success(true, "Experience updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ UpdateExperience failed");

                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }
    }

    #endregion
}

