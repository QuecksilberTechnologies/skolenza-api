using skolenza.application.DTOS.Common;
using skolenza.application.Exceptions;
using skolenza.application.Common.Enums;
using skolenza.application.Constants;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.StaffMemberCmd.ExperienceInfo.Handlers
{
    // ===============================
    // COMMAND
    // ===============================
    public class DeleteExperienceDocCommand : IRequest<ApiResponse<bool>>
    {
        public DeleteRequestDTO DTO { get; set; }

        public DeleteExperienceDocCommand(DeleteRequestDTO dto)
        {
            DTO = dto;
        }
    }

    // ===============================
    // HANDLER
    // ===============================
    public class DeleteExperienceDocCommandHandler
        : IRequestHandler<DeleteExperienceDocCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeleteExperienceDocCommandHandler> _logger;
        private readonly ICommonRequestService _commonRequestService;

        public DeleteExperienceDocCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<DeleteExperienceDocCommandHandler> logger,
            ICommonRequestService commonRequestService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _commonRequestService = commonRequestService;
        }

        public async Task<ApiResponse<bool>> Handle(
            DeleteExperienceDocCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("🚀 DeleteExperienceDoc started");

                // ===============================
                // 1️⃣ VALIDATION
                // ===============================
                var validation = await _commonRequestService.ValidateInstitutionUserRequestAsync();

                if (!validation.Success)
                    throw new UnauthorizedAccessException(validation.ErrorMessage);

                if (request?.DTO == null)
                    throw new ValidationErrorException("Invalid request.");

                if (request.DTO.Id <= 0)
                    throw new ValidationErrorException("Invalid DocumentId.");

                var loggedInStaffMemberId = validation.UserStaffMemberId;

                // ===============================
                // 2️⃣ FETCH DOCUMENT
                // ===============================
                var existing = await _unitOfWork.StaffMemberExperienceDocumentRepository
                    .GetSingleByDetailIdAsync(request.DTO.Id);

                if (existing == null)
                {
                    _logger.LogWarning("⚠️ Doc not found | Id: {Id}", request.DTO.Id);
                    throw new ApiException("Document not found.", 404);
                }

                // Resolve the document owner from the database; a document identifier alone
                // must never grant cross-staffMember access.
                var canAccessOwner = await _commonRequestService.CanAccessStaffMemberDataAsync(
                    validation,
                    existing.StaffMemberExperience.StaffMemberId,
                    StaffMemberDataAccessRequirement.PersonalDetails,
                    cancellationToken);
                if (!canAccessOwner)
                {
                    _logger.LogWarning(
                        "Experience document deletion denied. DocumentId: {DocumentId}, ActorStaffMemberId: {ActorStaffMemberId}, OwnerStaffMemberId: {OwnerStaffMemberId}",
                        request.DTO.Id,
                        loggedInStaffMemberId,
                        existing.StaffMemberExperience.StaffMemberId);
                    throw new ForbiddenAccessException(AppConstants.ErrorMessages.PermissionDenied);
                }

                // ===============================
                // 4️⃣ ALREADY DELETED CHECK
                // ===============================
                if (validation.RoleTypeId != ConstantValues.RoleTypeAdmin &&
                    (existing.StaffMemberExperience.IsInfoVerified == true ||
                     existing.StaffMemberExperience.IsEditAllowed != true))
                    throw new ForbiddenAccessException(AppConstants.ErrorMessages.PermissionDenied);

                if (existing.IsSoftDeleted)
                {
                    _logger.LogWarning("⚠️ Already deleted | Id: {Id}", request.DTO.Id);
                    throw new ApiException("Document already deleted.", 400);
                }

                // ===============================
                // 5️⃣ SOFT DELETE
                // ===============================
                existing.IsSoftDeleted = true;
                existing.IsActive = false;
                existing.SoftDeletedById = loggedInStaffMemberId;
                existing.UpdatedById = loggedInStaffMemberId;
                existing.UpdatedDateTime = DateTime.UtcNow;

                // ===============================
                // 6️⃣ UPDATE (NO SAVE HERE)
                // ===============================
                await  _unitOfWork.StaffMemberExperienceDocumentRepository.SoftDeleteAsync(existing);

                // ===============================
                // 7️⃣ SAVE (ONLY HERE)
                // ===============================
                var rows = await _unitOfWork.SaveChangesAsync(cancellationToken);

                if (rows <= 0)
                    throw new ApiException("Delete failed.", 500);

                // ===============================
                // 8️⃣ SUCCESS
                // ===============================
                _logger.LogInformation("✅ Doc deleted | Id: {Id} | User: {UserId}",
                    request.DTO.Id, loggedInStaffMemberId);

                return ApiResponse<bool>.Success(true, "Document deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ DeleteExperienceDoc failed | Id: {Id}", request.DTO?.Id);
                throw;
            }
        }
    }
}
