using skolenza.application.DTOS.Staff.Education;
using skolenza.application.Exceptions;
using skolenza.application.Constants;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IPermission;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.StaffMemberCmd.EducationInfo.Handlers
{
    public class DeleteEducationInfoQuery : IRequest<ApiResponse<bool>>
    {
        public DeleteEducationRequestDTO DTO { get; set; }

        public DeleteEducationInfoQuery(DeleteEducationRequestDTO dTO)
        {
            DTO = dTO;
        }
    }
    public class DeleteEducationInfoQueryHandler
  : IRequestHandler<DeleteEducationInfoQuery, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeleteEducationInfoQueryHandler> _logger;
        private readonly IPermissionService _permissionService;
        private readonly ICommonRequestService _commonRequestService;

        public DeleteEducationInfoQueryHandler(
            IUnitOfWork unitOfWork,
            ILogger<DeleteEducationInfoQueryHandler> logger,
            IPermissionService permissionService,
            ICommonRequestService commonRequestService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _permissionService = permissionService;
            _commonRequestService = commonRequestService;
        }

        public async Task<ApiResponse<bool>> Handle(
      DeleteEducationInfoQuery request,
      CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("DeleteEducation started");

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

                if (request.DTO.Id <= 0)
                    throw new ValidationErrorException("Invalid education record id.");

                long loggedInStaffMemberId = validation.UserStaffMemberId;
                // 4️⃣ FETCH RECORD
                // ===============================
                var existing =
                    await _unitOfWork.StaffMemberEducationRepository
                        .GetSingleRecordAsync(request.DTO.Id, true);

                if (existing == null)
                    throw new ApiException("Education record not found.", 404);

                // ===============================
                // 5️⃣ OWNERSHIP CHECK
                // ===============================
                if (existing.StaffMemberId != loggedInStaffMemberId)
                    throw new ForbiddenAccessException(AppConstants.ErrorMessages.PermissionDenied);

                if (existing.IsInfoVerified == true || existing.IsEditAllowed != true)
                    throw new ForbiddenAccessException(AppConstants.ErrorMessages.PermissionDenied);

                // ===============================
                // 6️⃣ SOFT DELETE
                // ===============================
                existing.IsSoftDeleted = true;
                existing.IsActive = false;
                existing.DeletedDateTime = DateTime.UtcNow;
                existing.SoftDeletedById = loggedInStaffMemberId;

                existing.FilePath = null;
                existing.FileName = null;
                existing.HasEducationDocUploded = false;
                existing.IsEditAllowed = false;

                var deleted =
                    await _unitOfWork.StaffMemberEducationRepository
                        .DeleteAsync(existing);

                if (!deleted)
                    throw new ApiException("Failed to delete education record.", 500);

                _logger.LogInformation("DeleteEducation success | Id: {Id}", request.DTO.Id);

                return ApiResponse<bool>.Success(true, "Education record deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error deleting education | Id: {Id}",
                    request.DTO?.Id);

                throw; // 🚨 MUST
            }
        }
    }




}
