using skolenza.application.DTOS.Staff.Bank;
using skolenza.application.Exceptions;
using skolenza.application.Constants;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IPermission;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.StaffMemberCmd.BankInfo.Handlers
{
    public class DeleteBankInfoQuery : IRequest<ApiResponse<bool>>
    {
        public DeleteBankRequestDTO DTO { get; }

        public DeleteBankInfoQuery(DeleteBankRequestDTO dto)
        {
            DTO = dto;
        }
    }


    public class DeleteBankInfoQueryHandler
    : IRequestHandler<DeleteBankInfoQuery, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeleteBankInfoQueryHandler> _logger;
        private readonly IPermissionService _permissionService;
        private readonly ICommonRequestService _commonRequestService;

        public DeleteBankInfoQueryHandler(
            IUnitOfWork unitOfWork,
            ILogger<DeleteBankInfoQueryHandler> logger,
            IPermissionService permissionService,
            ICommonRequestService commonRequestService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _permissionService = permissionService;
            _commonRequestService = commonRequestService;
        }

        public async Task<ApiResponse<bool>> Handle(
   DeleteBankInfoQuery request,
   CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("DeleteBankInfo started");

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
                    throw new ValidationErrorException("Invalid bank record id.");
                // 4️⃣ FETCH RECORD
                // ===============================
                var existing =
                    await _unitOfWork.StaffMemberBankRepository
                        .GetSingleRecordAsync(request.DTO.Id, true);

                if (existing == null)
                    throw new ApiException("Bank record not found.", 404);

                if (!await _commonRequestService.CanAccessStaffMemberDataAsync(
                        validation, existing.StaffMemberId, StaffMemberDataAccessRequirement.PersonalDetails, cancellationToken))
                {
                    throw new ForbiddenAccessException(AppConstants.ErrorMessages.PermissionDenied);
                }

                if (validation.RoleTypeId != ConstantValues.RoleTypeAdmin &&
                    (existing.IsInfoVerified || !existing.IsEditAllowed))
                {
                    throw new ForbiddenAccessException(AppConstants.ErrorMessages.PermissionDenied);
                }

                // ===============================
                // 5️⃣ SOFT DELETE
                // ===============================
                existing.IsSoftDeleted = true;
                existing.IsActive = false;
                existing.DeletedDateTime = DateTime.UtcNow;
                existing.SoftDeletedById = validation.UserStaffMemberId;

                existing.HasChequeDocUploaded = false;
                existing.FilePath = null;
                existing.FileName = null;
                existing.FileType = 0;

                existing.UpdatedById = validation.UserStaffMemberId;
                existing.UpdatedDateTime = DateTime.UtcNow;

                var deleted =
                    await _unitOfWork.StaffMemberBankRepository
                        .DeleteAsync(existing);

                if (!deleted)
                    throw new ApiException("Failed to delete bank record.", 500);

                _logger.LogInformation("DeleteBankInfo success | Id: {Id}", request.DTO.Id);

                return ApiResponse<bool>.Success(true, "Bank record deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error deleting bank record | BankId: {Id}",
                    request.DTO?.Id);

                throw; // 🚨 MUST (middleware handle karega)
            }
        }
    }



}
