using skolenza.application.DTOS.Common;
using skolenza.application.Exceptions;
using skolenza.application.Constants;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IPermission;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.StaffMemberCmd.DependentInfo.Handlers
{
    public class DeleteContactQuery : IRequest<ApiResponse<bool>>
    {
        public DeleteRequestDTO DTO;

        public DeleteContactQuery(DeleteRequestDTO dto)
        {
            DTO = dto;
        }
    }

    public class DeleteContactInfoQueryHandler
    : IRequestHandler<DeleteContactQuery, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeleteContactInfoQueryHandler> _logger;
        private readonly IPermissionService _permissionService;
        private readonly ICommonRequestService _commonRequestService;

        public DeleteContactInfoQueryHandler(
            IUnitOfWork unitOfWork,
            ILogger<DeleteContactInfoQueryHandler> logger,
            IPermissionService permissionService,
            ICommonRequestService commonRequestService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _permissionService = permissionService;
            _commonRequestService = commonRequestService;
        }

        public async Task<ApiResponse<bool>> Handle(
    DeleteContactQuery request,
    CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("DeleteDependent started");

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
                    throw new ValidationErrorException("Invalid dependent id.");

                long loggedInStaffMemberId = validation.UserStaffMemberId;
                // 4️⃣ FETCH RECORD
                // ===============================
                var existing =
                    await _unitOfWork.StaffMemberDependentRepository
                        .GetSingleRecordAsync(request.DTO.Id, true);

                if (existing == null)
                    throw new ApiException("Dependent record not found.", 404);

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

                var deleted =
                    await _unitOfWork.StaffMemberDependentRepository
                        .DeleteAsync(existing);

                if (!deleted)
                    throw new ApiException("Failed to delete dependent.", 500);

                _logger.LogInformation("DeleteDependent success | Id: {Id}", request.DTO.Id);

                return ApiResponse<bool>.Success(true, "Dependent deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error deleting dependent | Id: {Id}",
                    request.DTO?.Id);

                throw; // 🚨 MUST
            }
        }
    }
}
