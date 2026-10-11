// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles Get All Reporting Type Query Handler requests.
// ================================================================

using skolenza.application.DTOs.Manager.ReportingType;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.ReportTypeCmd.Handlers
{

    #region Query

    /// <summary>
    /// Represents the GetAllReportingTypeQuery application component.
    /// </summary>
    public class GetAllReportingTypeQuery
        : IRequest<ApiResponse<List<GetReportingTypeResponseDTO>>>
    {
        public GetReportingTypeRequestDTO DTO { get; set; }

        public GetAllReportingTypeQuery(GetReportingTypeRequestDTO dto)
        {
            DTO = dto;
        }
    }

    /// <summary>
    /// Handles GetAllReportingTypeQuery requests.
    /// </summary>
        #endregion

    #region Handler

public class GetAllReportingTypeQueryHandler
        : IRequestHandler<GetAllReportingTypeQuery, ApiResponse<List<GetReportingTypeResponseDTO>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetAllReportingTypeQueryHandler> _logger;
        private readonly ICommonRequestService _commonRequestService;

        public GetAllReportingTypeQueryHandler(
            IUnitOfWork unitOfWork,
            ILogger<GetAllReportingTypeQueryHandler> logger,
            ICommonRequestService commonRequestService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _commonRequestService = commonRequestService;
        }

        public async Task<ApiResponse<List<GetReportingTypeResponseDTO>>> Handle(
            GetAllReportingTypeQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("🔹 GetAllReportingType started");

                // ===============================
                // 1️⃣ VALIDATION
                // ===============================
                var validation = await _commonRequestService.ValidateInstitutionUserRequestAsync();

                if (!validation.Success)
                    throw new UnauthorizedAccessException(validation.ErrorMessage);

                // ===============================
                // 2️⃣ PERMISSION (MANDATORY)
                // ===============================
                //var hasAccess = await _commonRequestService.HasAccessAsync(
                //    validation.RoleId,
                //    Modules.ReportingType,
                //    Operations.View);

                //if (!hasAccess)
                //    throw new UnauthorizedAccessException("Access denied for view operation.");

                // ===============================
                // 3️⃣ NULL CHECK
                // ===============================
                if (request?.DTO == null)
                    throw new Exception("Invalid request data.");

                // ===============================
                // 4️⃣ REPOSITORY CALL (PAGINATION)
                // ===============================
                var result = await _unitOfWork.ReportingTypeRepository
                    .AllAsync(validation.InstitutionId, request.DTO);

                if (result == null)
                    throw new Exception("Failed to fetch ReportingTypes.");

                // ===============================
                // 5️⃣ SUCCESS RESPONSE (PAGINATED)
                // ===============================
                return ApiResponse<List<GetReportingTypeResponseDTO>>
                    .SuccessPaginatedOnly(
                        Data: result.Data,
                        PageNumber: result.PageNumber,
                        PageSize: result.PageSize,
                        TotalRecords: result.TotalCount,
                        TotalPages: result.TotalPages,
                        Message: result.Data.Any()
                            ? "ReportingTypes retrieved successfully."
                            : "No ReportingTypes found."
                    );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllReportingType");
                throw; //  middleware handle karega
            }
        }
    }
    #endregion
}
