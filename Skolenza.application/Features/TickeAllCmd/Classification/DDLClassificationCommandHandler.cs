using skolenza.application.DTOS.Common;
using skolenza.application.DTOS.Pagination;
using skolenza.application.DTOS.TicketDTO.Classification;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IPermission;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.TickeAllCmd.Classification
{
    // ===============================
    // COMMAND
    // ===============================
    public class DDLClassificationCommand: IRequest<ApiResponse<List<GetClassificationResponseDTO>>>
    {
        public DDLClassificationRequestDTO DTO { get; }

        public DDLClassificationCommand(DDLClassificationRequestDTO dto)
        {
            DTO = dto;
        }
    }

    // ===============================
    // HANDLER (MASTER PATTERN)
    // ===============================
    public class DDLClassificationCommandHandler
     : IRequestHandler<DDLClassificationCommand, ApiResponse<List<GetClassificationResponseDTO>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DDLClassificationCommandHandler> _logger;
        private readonly ICommonRequestService _commonRequestService;

        public DDLClassificationCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<DDLClassificationCommandHandler> logger,
            ICommonRequestService commonRequestService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _commonRequestService = commonRequestService;
        }

        public async Task<ApiResponse<List<GetClassificationResponseDTO>>> Handle(
            DDLClassificationCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                // ===============================
                // 1️⃣ VALIDATION
                // ===============================
                var validation = await _commonRequestService.ValidateInstitutionUserRequestAsync();

                if (!validation.Success)
                    throw new UnauthorizedAccessException(validation.ErrorMessage);

                // ===============================
                // 2️⃣ NULL SAFETY
                // ===============================
                if (request?.DTO == null)
                    throw new ValidationErrorException("Invalid request.");

                // ===============================
                // 3️⃣ REPOSITORY CALL
                // ===============================
                var result = await _unitOfWork.TicketClassificationRepository
                    .GetDDLAsync(request.DTO.IsActive, validation.InstitutionId);

                // ===============================
                // 4️⃣ RESPONSE
                // ===============================
                return ApiResponse<List<GetClassificationResponseDTO>>
                    .Success(result, "Classification dropdown fetched successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching classification DDL");
                throw;
            }
        }
    }

}