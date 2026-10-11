// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Handles UpdateContactInfoCommandHandler requests using authenticated institution context.
// ================================================================

using skolenza.application.Common.Helpers.Converters;
using skolenza.application.Common.Helpers;
using skolenza.application.Common.Helpers.RequestHelper;
using skolenza.application.Constants;
using skolenza.application.DTOs.Module;
using skolenza.application.DTOS.Staff.Contact;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Wrappers;
using skolenza.domain.Entity; using MediatR;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text.RegularExpressions;


public class UpdateStaffMemberContactCommand : IRequest<ApiResponse<bool>> 
       { 
      public UpdateContactRequestDTO DTO { get; set; } 
      public UpdateStaffMemberContactCommand(UpdateContactRequestDTO dto) { DTO = dto; } 
       
       }

/// <summary>
/// Handles authenticated institution requests for this feature.
/// </summary>
public class UpdateContactInfoCommandHandler
   : IRequestHandler<UpdateStaffMemberContactCommand, ApiResponse<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateContactInfoCommandHandler> _logger;
    private readonly ICommonRequestService _commonRequestService;

    public UpdateContactInfoCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<UpdateContactInfoCommandHandler> logger,
        ICommonRequestService commonRequestService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _commonRequestService = commonRequestService;
    }

    public async Task<ApiResponse<bool>> Handle(
    UpdateStaffMemberContactCommand request,
    CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("UpdateContact started");

            // ===============================
            // 1️⃣ VALIDATION
            // ===============================
            #region Institution Request Validation
            var validation =
                await _commonRequestService.ValidateInstitutionUserRequestAsync();
            #endregion

            if (!validation.Success)
                throw new UnauthorizedAccessException(validation.ErrorMessage);

            // ===============================
            // 2️⃣ NULL SAFETY
            // ===============================
            if (request?.DTO == null)
                throw new ValidationErrorException("Invalid request.");

            if (request.DTO.Id <= 0)
                throw new ValidationErrorException("Invalid contact id.");

            long loggedInStaffMemberId = validation.UserStaffMemberId;
            // 4️⃣ FETCH EXISTING
            // ===============================
            var existing =
                await _unitOfWork.StaffMemberContactRepository
                    .GetSingleRecordAsync(request.DTO.Id, true);

            if (existing == null)
                throw new ApiException("Contact record not found.", 404);

            // ===============================
            // 5️⃣ OWNERSHIP CHECK
            // ===============================
            if (existing.StaffMemberId != loggedInStaffMemberId)
                throw new ForbiddenAccessException(AppConstants.ErrorMessages.PermissionDenied);

            if (existing.IsInfoVerified == true || existing.IsEditAllowed != true)
                throw new ForbiddenAccessException(AppConstants.ErrorMessages.PermissionDenied);

            // ===============================
            // 6️⃣ START TRANSACTION
            // ===============================
            await _unitOfWork.BeginTransactionAsync();

            var dto = request.DTO;

            // ===============================
            // 7️⃣ APPLY UPDATES
            // ===============================

            if (!string.IsNullOrWhiteSpace(dto.ContactName))
                existing.ContactName = dto.ContactName.Trim();

            if (dto.ContactType.HasValue && dto.ContactType > 0)
                existing.ContactType = dto.ContactType.Value;

            if (dto.Relation.HasValue && dto.Relation > 0)
                existing.Relation = dto.Relation.Value;

            if (!string.IsNullOrWhiteSpace(dto.ContactNumber))
            {
                var clean = dto.ContactNumber.Trim();
                StaffMemberProfileValidationHelper.ValidateContactNumber(clean, "contact number");

                existing.ContactNumber = clean;
            }

            if (!string.IsNullOrWhiteSpace(dto.AlternateNumber))
            {
                var clean = dto.AlternateNumber.Trim();
                StaffMemberProfileValidationHelper.ValidateContactNumber(clean, "alternate number");

                existing.AlternateNumber = clean;
            }

            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                var email = dto.Email.Trim();
                if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                    throw new ValidationErrorException("Invalid email format.");

                existing.Email = email;
            }

            if (dto.IsPrimary.HasValue)
                existing.IsPrimary = dto.IsPrimary.Value;

            if (dto.CountryId.HasValue)
                existing.CountryId = dto.CountryId.Value;

            if (dto.StateId.HasValue)
                existing.StateId = dto.StateId.Value;

            if (dto.DistrictId.HasValue)
                existing.DistrictId = dto.DistrictId.Value;

            if (dto.LocalityId.HasValue)
                existing.LocalityId = dto.LocalityId.Value;

            if (dto.HouseNo != null)
                existing.HouseNo = dto.HouseNo.Trim();

            if (dto.Street != null)
                existing.Street = dto.Street.Trim();

            if (dto.LandMark != null)
                existing.LandMark = dto.LandMark.Trim();

            if (dto.Address != null)
                existing.Address = dto.Address.Trim();

            if (dto.Description != null)
                existing.Description = dto.Description.Trim();

            // ===============================
            // 🧾 AUDIT
            // ===============================
            existing.UpdatedById = loggedInStaffMemberId;
            existing.UpdatedDateTime = DateTime.UtcNow;

            // ===============================
            // 8️⃣ SAVE
            // ===============================
            var updated =
                await _unitOfWork.StaffMemberContactRepository
                    .UpdateContactAsync(existing);

            if (!updated)
                throw new ApiException("Failed to update contact.", 500);

            // ===============================
            // 9️⃣ COMMIT
            // ===============================
            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation("UpdateContact success");

            return ApiResponse<bool>.Success(true, "Contact updated successfully.");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();

            _logger.LogError(ex, "Error updating contact");

            throw; // 🚨 MUST
        }
    }
}
