using skolenza.application.DTOs.Manager.ReportingType;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.ReportTypeCmd.Handlers
{
    public class GetReportingTypeByIdQuery
        : IRequest<ApiResponse<GetReportingTypeResponseDTO>>
    {
        public GetReportingTypeByIdRequestDTO DTO { get; set; }

        public GetReportingTypeByIdQuery(GetReportingTypeByIdRequestDTO dto)
        {
            DTO = dto;
        }
    }

    public class GetReportingTypeByIdQueryHandler
        : IRequestHandler<GetReportingTypeByIdQuery, ApiResponse<GetReportingTypeResponseDTO>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetReportingTypeByIdQueryHandler> _logger;
        private readonly ICommonRequestService _commonRequestService;

        public GetReportingTypeByIdQueryHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetReportingTypeByIdQueryHandler> logger,
            ICommonRequestService commonRequestService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _commonRequestService = commonRequestService;
        }

        public async Task<ApiResponse<GetReportingTypeResponseDTO>> Handle(
            GetReportingTypeByIdQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("🔹 GetReportingTypeById started");

                // ===============================
                // 1️⃣ VALIDATION
                // ===============================
                var validation = await _commonRequestService.ValidateInstitutionUserRequestAsync();

                if (!validation.Success)
                    throw new UnauthorizedAccessException(validation.ErrorMessage);

                // ===============================
                // 2️⃣ PERMISSION (MANDATORY)
                //// ===============================
                //var hasAccess = await _commonRequestService.HasAccessAsync(
                //    validation.RoleId,
                //    Modules.ReportingType,
                //    Operations.View);

                //if (!hasAccess)
                //    throw new UnauthorizedAccessException("Access denied for view operation.");

                // ===============================
                // 3️⃣ NULL CHECK
                

                // ===============================
                // 5️⃣ REPOSITORY CALL
                // ===============================
                var result = await _unitOfWork.ReportingTypeRepository
                    .GetByIdAsync(request.DTO.Id);

                if (result == null)
                    throw new Exception("ReportingType not found.");

                // ===============================
                // 6️⃣ SUCCESS
                // ===============================
                return ApiResponse<GetReportingTypeResponseDTO>
                    .Success(result, "ReportingType retrieved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetReportingTypeById");
                throw;
            }
        }
    }
}