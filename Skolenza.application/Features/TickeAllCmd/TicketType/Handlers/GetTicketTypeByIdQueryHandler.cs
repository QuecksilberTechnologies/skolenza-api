using AutoMapper;
using skolenza.application.DTOS.TicketDTO.TicketType;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.TickeAllCmd.TicketType.Handlers
{
    public class GetTicketTypeByIdQuery : IRequest<ApiResponse<GetTicketTypeResponseDTO>>
    {
        public GetTicketTypeByIdRequestDTO DTO { get; set; }

        public GetTicketTypeByIdQuery(GetTicketTypeByIdRequestDTO dTO)
        {
            DTO = dTO;
        }
    }
        public class GetTicketTypeByIdQueryHandler : IRequestHandler<GetTicketTypeByIdQuery, ApiResponse<GetTicketTypeResponseDTO>>
    {
        private readonly ITicketTypeRepository _repository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStoreProcedureRepository _commonRepository;
        private readonly ILogger<GetTicketTypeByIdQueryHandler> _logger;
        private readonly ICommonRequestService _commonRequestService;

        public GetTicketTypeByIdQueryHandler(
            ITicketTypeRepository repository,
            IMapper mapper,
            IUnitOfWork unitOfWork,
            IStoreProcedureRepository commonRepository,
            ICommonRequestService commonRequestService,
            ILogger<GetTicketTypeByIdQueryHandler> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _commonRequestService = commonRequestService ?? throw new ArgumentNullException(nameof(commonRequestService));
            _commonRepository = commonRepository ?? throw new ArgumentNullException(nameof(commonRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ApiResponse<GetTicketTypeResponseDTO>> Handle(GetTicketTypeByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("🔍 GetAllTicketTypeByHeaderId started. HeaderId: {HeaderId}", request?.DTO?.Id);

                // ===============================
                // 1️⃣ VALIDATION
                // ===============================
                var validation = await _commonRequestService.ValidateInstitutionUserRequestAsync();

                if (!validation.Success)
                    throw new UnauthorizedAccessException(validation.ErrorMessage);

                if (request?.DTO == null)
                    throw new ValidationErrorException("Invalid request.");

                
                // 2️⃣ Fetch from repository
                var Entity = await _repository.GetByIdAsync(request.DTO.Id, request.DTO.IsActive);
                if (Entity == null)
                {
                    _logger.LogWarning("⚠️ No TicketType found for Id = {Id}", request.DTO.Id);
                    return new ApiResponse<GetTicketTypeResponseDTO>
                    {
                        IsSucceeded = false,
                        Message = $"No TicketType found for Id = {request.DTO.Id}.",
                        Data = null
                    };
                }
 

                

                _logger.LogInformation("✅ Successfully fetched TicketType (Id = {Id})", request.DTO.Id);

                // 5️⃣ Success response
                return new ApiResponse<GetTicketTypeResponseDTO>
                {
                    IsSucceeded = true,
                    Message = "TicketType fetched successfully.",
                    Data = Entity
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error occurred while fetching TicketType by Id = {Id}", request.DTO?.Id);

                return new ApiResponse<GetTicketTypeResponseDTO>
                {
                    IsSucceeded = false,
                    Message = $"An unexpected error occurred while fetching TicketType: {ex.Message}",
                    Data = null
                };
            }
        }
    }
}
