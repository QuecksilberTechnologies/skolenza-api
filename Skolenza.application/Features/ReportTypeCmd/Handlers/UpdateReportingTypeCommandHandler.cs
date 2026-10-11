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
    public class UpdateReportingTypeCommand : IRequest<ApiResponse<bool>>
    {
        public UpdateReportingTypeRequestDTO DTO { get; set; }

        public UpdateReportingTypeCommand(UpdateReportingTypeRequestDTO dto)
        {
            DTO = dto;
        }
    }

    // ✅ Handler
    public class UpdateReportingTypeCommandHandler
        : IRequestHandler<UpdateReportingTypeCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UpdateReportingTypeCommandHandler> _logger;
        private readonly ICommonRequestService _commonRequestService;

        public UpdateReportingTypeCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<UpdateReportingTypeCommandHandler> logger,
            ICommonRequestService commonRequestService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _commonRequestService = commonRequestService;
        }

        public async Task<ApiResponse<bool>> Handle(
            UpdateReportingTypeCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("🔹 UpdateReportingType started");

                // ===============================
                // 1️⃣ VALIDATION
                // ===============================
                var validation = await _commonRequestService.ValidateInstitutionUserRequestAsync();

                if (!validation.Success)
                    throw new UnauthorizedAccessException(validation.ErrorMessage);
               
                //var hasPermission = await _permissionService.HasAccessAsync(
                //    validation.RoleId,
                //    "TicketClassification",   // ModuleName (DB match)
                //    "Delete"                  // Operation
                //);

                //if (!hasPermission)
                //    throw new UnauthorizedAccessException("You do not have permission to delete classification.");


                // ===============================
                // ===============================
                // 2️⃣ NULL CHECK
                // ===============================
                if (request?.DTO == null)
                    throw new ValidationErrorException("Invalid request data.");

               

                // ===============================
                // 3️⃣ UPDATE
                // ===============================
                var isUpdated = await _unitOfWork.ReportingTypeRepository
                    .UpdateAsync(request.DTO);

                if (!isUpdated)
                    throw new Exception("ReportingType update failed. Either not found or no changes detected.");

                // ===============================
                // 4️⃣ COMMIT
                // ===============================
                await _unitOfWork.CommitAsync();

                _logger.LogInformation("ReportingType updated successfully with Id {Id}", request.DTO.Id);

                // ===============================
                // 5️⃣ SUCCESS
                // ===============================
                return ApiResponse<bool>
                    .Success(true, "ReportingType updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateReportingType with Id {Id}", request.DTO?.Id);
                throw; // ✅ middleware handle karega
            }
        }
    }
}