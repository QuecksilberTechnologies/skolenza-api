using AutoMapper;
using skolenza.application.DTOS.TicketDTO.Classification;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;
using Microsoft.Extensions.Logging;


namespace skolenza.application.Features.TickeAllCmd.Classification
{
    public class GetClassificationByIdQuery : IRequest<ApiResponse<GetClassificationResponseDTO>>
    {
        public GetClassificationRequestDTO Dto { get; set; }

        public GetClassificationByIdQuery(GetClassificationRequestDTO dto)
        {
            Dto = dto;
        }
    }
    public class GetClassificationByIdQueryHandler : IRequestHandler<GetClassificationByIdQuery, ApiResponse<GetClassificationResponseDTO>>
    {
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICommonRequestService _commonRequestService;
        private readonly ILogger<GetClassificationByIdQueryHandler> _logger;

        public GetClassificationByIdQueryHandler(
            IMapper mapper,
            IUnitOfWork unitOfWork,ICommonRequestService commonRequestService,
            ILogger<GetClassificationByIdQueryHandler> logger)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _commonRequestService = commonRequestService;
        }

        public async Task<ApiResponse<GetClassificationResponseDTO>> Handle(
            GetClassificationByIdQuery request,
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
                // 3️⃣ RBAC (OPTIONAL)
                // ===============================
                // await _commonRequestService.HasAccessAsync(
                //     ModuleEnum.Ticket,
                //     OperationEnum.View);

                // ===============================
                // 2️⃣ NULL SAFETY
                // ===============================
                if (request?.Dto == null || request.Dto.Id <= 0)
                    throw new ValidationErrorException("Invalid request.");

                // ===============================
                // 3️⃣ REPOSITORY CALL
                // ===============================
                var result = await _unitOfWork.TicketClassificationRepository
                    .GetByIdAsync(request.Dto.Id, validation.InstitutionId);

                if (result == null)
                    throw new ApiException("Classification not found.", 404);

                // ===============================
                // 4️⃣ RESPONSE
                // ===============================
                return ApiResponse<GetClassificationResponseDTO>
                    .Success(result, "Classification fetched successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching classification by Id: {Id}", request?.Dto?.Id);
                throw;
            }
        }
    }
}
