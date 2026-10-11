using skolenza.application.DTOS.Common;
using skolenza.application.Exceptions;
using skolenza.application.Constants;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace skolenza.application.Features.StaffMemberCmd.ExperienceInfo.Handlers
{
    public class DeleteExperienceCommand : IRequest<ApiResponse<bool>>
    {
        public DeleteRequestDTO DTO { get; set; }

        public DeleteExperienceCommand(DeleteRequestDTO dto)
        {
            DTO = dto;
        }
    }

    public class DeleteExperienceCommandHandler
        : IRequestHandler<DeleteExperienceCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeleteExperienceCommandHandler> _logger;
        private readonly ICommonRequestService _commonRequestService;
        private readonly IIdEncoderService _idEncoderService;

        public DeleteExperienceCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<DeleteExperienceCommandHandler> logger,
            ICommonRequestService commonRequestService,
            IIdEncoderService idEncoderService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _commonRequestService = commonRequestService;
            _idEncoderService = idEncoderService;
        }

        public async Task<ApiResponse<bool>> Handle(
            DeleteExperienceCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("🚀 DeleteExperience started");

                // ===============================
                // 1️⃣ VALIDATION
                // ===============================
                var validation = await _commonRequestService.ValidateInstitutionUserRequestAsync();

                if (!validation.Success)
                    throw new UnauthorizedAccessException(validation.ErrorMessage);

                if (request?.DTO == null)
                    throw new ValidationErrorException("Invalid request.");               

                 
                if (request.DTO.Id <= 0)
                    throw new ValidationErrorException("Invalid ExperienceId.");

                var loggedInStaffMemberId = validation.UserStaffMemberId;

                // ===============================
                // 3️⃣ FETCH RECORD
                // ===============================
              
                var existing = await _unitOfWork.StaffMemberExperienceRepository
                  .GetByIdAsync(request.DTO.Id, loggedInStaffMemberId);

                if (existing == null)
                {
                    _logger.LogWarning("⚠️ DeleteExperience failed | Reason: Record not found | Id: {Id}", request.DTO.Id);
                    throw new ApiException("Experience record not found.", 404);
                }

                // ===============================
                // 4️⃣ OWNERSHIP CHECK
                // ===============================
                if (existing.StaffMemberId != loggedInStaffMemberId)
                {
                    _logger.LogWarning("⚠️ DeleteExperience unauthorized | Id: {Id} | User: {UserId}", request.DTO.Id, loggedInStaffMemberId);
                    throw new ForbiddenAccessException(AppConstants.ErrorMessages.PermissionDenied);
                }

                // ===============================
                // 5️⃣ ALREADY DELETED CHECK 🔥
                // ===============================
                if (existing.IsInfoVerified == true || existing.IsEditAllowed != true)
                    throw new ForbiddenAccessException(AppConstants.ErrorMessages.PermissionDenied);

                if (existing.IsSoftDeleted  )
                {
                    _logger.LogWarning("⚠️ DeleteExperience skipped | Already deleted | Id: {Id}", request.DTO.Id);
                    throw new ApiException("Experience already deleted.", 400);
                }

                // ===============================
                // 6️⃣ SOFT DELETE
                // ===============================
                existing.IsSoftDeleted = true;
                existing.IsActive = false;
                existing.SoftDeletedById = loggedInStaffMemberId;
                existing.UpdatedById = loggedInStaffMemberId;
                existing.UpdatedDateTime = DateTime.UtcNow;

                // ===============================
                // 7️⃣ SAVE
                // ===============================
                var isDeleted = await _unitOfWork.StaffMemberExperienceRepository
                    .SoftDeleteAsync(existing);

                if (!isDeleted)
                {
                    _logger.LogError("❌ DeleteExperience failed during DB save | Id: {Id}", request.DTO.Id);
                    throw new ApiException("Failed to delete experience.", 500);
                }

                // ===============================
                // 8️⃣ SUCCESS
                // ===============================
                _logger.LogInformation("✅ Experience deleted successfully | Id: {Id} | User: {UserId}", request.DTO.Id, loggedInStaffMemberId);

                return ApiResponse<bool>.Success(true, "Experience deleted successfully.");

 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error deleting experience | Id: {Id}", request.DTO?.Id);
                throw;
            }
        }
    }
}
