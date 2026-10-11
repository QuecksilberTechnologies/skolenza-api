using skolenza.application.DTOs.Manager.ReportingType;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.ReportTypeCmd.Handlers
{
    // ✅ Command
    public class DeleteReportingTypeCommand : IRequest<ApiResponse<bool>>
    {
        public DeleteReportingTypeRequestDTO DTO { get; set; }

        public DeleteReportingTypeCommand(DeleteReportingTypeRequestDTO dto)
        {
            DTO = dto;
        }
    }

    // ✅ Handler
    public class DeleteReportingTypeCommandHandler
        : IRequestHandler<DeleteReportingTypeCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeleteReportingTypeCommandHandler> _logger;
        private readonly ICommonRequestService _commonRequestService;

        public DeleteReportingTypeCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<DeleteReportingTypeCommandHandler> logger,
            ICommonRequestService commonRequestService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _commonRequestService = commonRequestService;
        }

        public async Task<ApiResponse<bool>> Handle(
            DeleteReportingTypeCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("🔹 DeleteReportingType started");

                // ===============================
                // 1️⃣ VALIDATION
                // ===============================
                var validation = await _commonRequestService.ValidateInstitutionUserRequestAsync();

                if (!validation.Success)
                    throw new UnauthorizedAccessException(validation.ErrorMessage);

                // ===============================
                // 2️⃣ PERMISSION CHECK (MANDATORY)
                // ===============================
                //var hasAccess = await _commonRequestService.HasAccessAsync(
                //    validation.RoleId,
                //    Modules.ReportingType,
                //    Operations.Delete);

                //if (!hasAccess)
                //    throw new UnauthorizedAccessException("Access denied for delete operation.");

                // ===============================
                // 3️⃣ NULL CHECK
                // ===============================
                if (request?.DTO == null || request.DTO.Id <= 0)
                    throw new ValidationErrorException("Invalid request data.");

                // ===============================
                // 4️⃣ DELETE (ID BASED)
                // ===============================
                var isDeleted = await _unitOfWork.ReportingTypeRepository
                    .DeleteAsync(request.DTO.Id, validation.UserStaffMemberId);

                if (!isDeleted)
                    throw new Exception("ReportingType delete failed or record not found.");

                // ===============================
                // 5️⃣ COMMIT
                // ===============================
                await _unitOfWork.CommitAsync();

                _logger.LogInformation(
                    "ReportingType deleted successfully. Id: {Id}, User: {UserId}",
                    request.DTO.Id,
                    validation.UserStaffMemberId);

                // ===============================
                // 6️⃣ SUCCESS
                // ===============================
                return ApiResponse<bool>
                    .Success(true, "ReportingType deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteReportingType with Id {Id}", request.DTO?.Id);
                throw; // ✅ middleware handle karega
            }
        }
    }
}