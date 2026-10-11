// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles Get Header By Classify Id Query Handler requests.
// ================================================================

using skolenza.application.DTOS.TicketDTO.Header;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.TickeAllCmd.TicketHeader.Handlers
{

    #region Query

    /// <summary>
    /// Represents the GetHeaderByClassifyIdQuery application component.
    /// </summary>
    public class GetHeaderByClassifyIdQuery : IRequest<ApiResponse<List<GetHeaderResponseDTO>>>
    {
        public GetTicketHeaderByClassifyIdRequestDTO Request { get; set; }

        public GetHeaderByClassifyIdQuery(GetTicketHeaderByClassifyIdRequestDTO request)
        {
            Request = request;
        }
    }

    /// <summary>
    /// Handles GetHeaderByClassifyIdQuery requests.
    /// </summary>
        #endregion

    #region Handler

public class GetHeaderByClassifyIdQueryHandler
        : IRequestHandler<GetHeaderByClassifyIdQuery, ApiResponse<List<GetHeaderResponseDTO>>>
    {
        private readonly ITicketHeaderRepository _repository;
        private readonly ILogger<GetHeaderByClassifyIdQueryHandler> _logger;
        private readonly ICommonRequestService _commonRequestService;

        public GetHeaderByClassifyIdQueryHandler(
            ITicketHeaderRepository repository,
            ICommonRequestService commonRequestService,
            ILogger<GetHeaderByClassifyIdQueryHandler> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _commonRequestService = commonRequestService ?? throw new ArgumentNullException(nameof(commonRequestService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ApiResponse<List<GetHeaderResponseDTO>>> Handle(
            GetHeaderByClassifyIdQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("🔍 GetHeaderByClassifyId started. ClassificationId: {Id}",
                    request?.Request.TicketClassifyId);

                // ===============================
                // 1️⃣ VALIDATION
                // ===============================
                var validation = await _commonRequestService.ValidateInstitutionUserRequestAsync();

                if (!validation.Success)
                    throw new UnauthorizedAccessException(validation.ErrorMessage);

                if (request?.Request == null)
                    throw new ValidationErrorException("Invalid request.");

                // ===============================
                // 2️⃣ RBAC (OPTIONAL but recommended)
                // ===============================
                // var hasAccess = await _commonRequestService.HasAccessAsync(
                //     validation.RoleId,
                //     Modules.Ticket,
                //     Operations.View);
                //
                // if (!hasAccess)
                //     throw new UnauthorizedAccessException("No permission");

                // ===============================
                // 3️⃣ REPOSITORY CALL
                // ===============================
                var headers = await _repository.GetByClassificationIdAsync(
                    validation.InstitutionId,
                    request.Request);

                if (headers == null || !headers.Any())
                    throw new ApiException("No headers found for given classification.", 404);

                // ===============================
                // 4️⃣ SUCCESS
                // ===============================
                _logger.LogInformation("✅ Retrieved {Count} headers", headers.Count);

                return new ApiResponse<List<GetHeaderResponseDTO>>
                {
                    IsSucceeded = true,
                    Message = $"Successfully fetched {headers.Count} headers.",
                    Data = headers
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in GetHeaderByClassifyId");
                throw; // middleware handle karega
            }
        }
    }
    #endregion
}
