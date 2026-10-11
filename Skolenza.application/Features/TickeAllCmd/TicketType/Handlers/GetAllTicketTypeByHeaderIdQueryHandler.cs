// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles Get All Ticket Type By Header Id Query Handler requests.
// ================================================================

using AutoMapper;
using skolenza.application.DTOS.TicketDTO.TicketType;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.TickeAllCmd.TicketType.Handlers
{

    #region Query

    /// <summary>
    /// Represents the GetAllTicketTypeByHeaderIdQuery application component.
    /// </summary>
    public class GetAllTicketTypeByHeaderIdQuery : IRequest<ApiResponse<List<GetTicketTypeResponseDTO>>>
    {
        public GetTicketTypeByHeaderIdRequestDTO Request { get; set; }

        public GetAllTicketTypeByHeaderIdQuery(GetTicketTypeByHeaderIdRequestDTO request)
        {
            Request = request;
        }
    }
    /// <summary>
    /// Handles GetAllTicketTypeByHeaderIdQuery requests.
    /// </summary>
        #endregion

    #region Handler

public class GetAllTicketTypeByHeaderIdQueryHandler
     : IRequestHandler<GetAllTicketTypeByHeaderIdQuery, ApiResponse<List<GetTicketTypeResponseDTO>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetAllTicketTypeByHeaderIdQueryHandler> _logger;
        private readonly ICommonRequestService _commonRequestService;

        public GetAllTicketTypeByHeaderIdQueryHandler(
            IUnitOfWork unitOfWork,
            ICommonRequestService commonRequestService,
            ILogger<GetAllTicketTypeByHeaderIdQueryHandler> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _commonRequestService = commonRequestService ?? throw new ArgumentNullException(nameof(commonRequestService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ApiResponse<List<GetTicketTypeResponseDTO>>> Handle(
            GetAllTicketTypeByHeaderIdQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("🔍 GetAllTicketTypeByHeaderId started. HeaderId: {HeaderId}", request?.Request?.TicketHeaderId);

                // ===============================
                // 1️⃣ VALIDATION
                // ===============================
                var validation = await _commonRequestService.ValidateInstitutionUserRequestAsync();

                if (!validation.Success)
                    throw new UnauthorizedAccessException(validation.ErrorMessage);

                if (request?.Request == null)
                    throw new ValidationErrorException("Invalid request.");

                // ===============================
                // 2️⃣ RBAC CHECK (MANDATORY )
                // ===============================
                //var hasAccess = await _commonRequestService.HasAccessAsync(
                //    validation.RoleId,
                //    Modules.Ticket,
                //    Operations.View);

                //if (!hasAccess)
                //    throw new UnauthorizedAccessException("You do not have permission to view ticket types.");

                // ===============================
                // 3️⃣ REPOSITORY CALL
                // ===============================
                var ticketTypes = await _unitOfWork.TicketTypeRepository
                    .AllByHeaderIdAsync(validation.InstitutionId, request.Request);

                if (ticketTypes == null || !ticketTypes.Any())
                    throw new ApiException("No TicketTypes found for given HeaderId.", 404);

                // ===============================
                // 4️⃣ SUCCESS RESPONSE
                // ===============================
                _logger.LogInformation("✅ Retrieved {Count} TicketTypes for HeaderId: {HeaderId}",
                    ticketTypes.Count, request.Request.TicketHeaderId);

                return new ApiResponse<List<GetTicketTypeResponseDTO>>
                {
                    IsSucceeded = true,
                    Message = $"Successfully fetched {ticketTypes.Count} TicketTypes.",
                    Data = ticketTypes
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in GetAllTicketTypeByHeaderId. HeaderId: {HeaderId}",
                    request?.Request?.TicketHeaderId);

                throw; //  Middleware handle karega (as per your architecture)
            }
        }
    }

    #endregion
}
