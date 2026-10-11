using AutoMapper;
using skolenza.application.DTOS.TicketDTO.TicketType;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.TickeAllCmd.TicketType.Handlers
{
    public class GetAllTicketTypeByRoleIdQuery : IRequest<ApiResponse<List<GetTicketTypeRoleResponseDTO>>>
    {
        public GetTicketTypeByRoleIdRequestDTO DTO { get; set; }

        public GetAllTicketTypeByRoleIdQuery(GetTicketTypeByRoleIdRequestDTO dto)
        {
            DTO = dto;
        }
    }
    public class GetAllTicketTypeByRoleIdQueryHandler : IRequestHandler<GetAllTicketTypeByRoleIdQuery, ApiResponse<List<GetTicketTypeRoleResponseDTO>>>
    {
        private readonly ITicketTypeRepository _repository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStoreProcedureRepository _commonRepository;
        private readonly ILogger<GetAllTicketTypeByRoleIdQueryHandler> _logger;

        public GetAllTicketTypeByRoleIdQueryHandler(
            ITicketTypeRepository repository,
            IMapper mapper,
            IUnitOfWork unitOfWork,
            IStoreProcedureRepository commonRepository,
            ILogger<GetAllTicketTypeByRoleIdQueryHandler> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _commonRepository = commonRepository ?? throw new ArgumentNullException(nameof(commonRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        

        public async Task<ApiResponse<List<GetTicketTypeRoleResponseDTO>>> Handle(GetAllTicketTypeByRoleIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // 1️⃣ Validation
                if (request == null || request.DTO == null)
                {
                    _logger.LogWarning("⚠️ Invalid request received in GetAllTicketTypeByRoleIdQuery.");
                    return new ApiResponse<List<GetTicketTypeRoleResponseDTO>>
                    {
                        IsSucceeded = false,
                        Message = "Invalid request. Please provide valid module information.",
                        Data = new List<GetTicketTypeRoleResponseDTO>()
                    };
                }

                // 2️⃣ Extract RoleId & Active filter
                var RoleId = request.DTO.ResponsibleRoleId;
               
                if (RoleId <= 0)
                {
                    _logger.LogWarning("⚠️ Invalid RoleId received: {RoleId}", RoleId);
                    return new ApiResponse<List<GetTicketTypeRoleResponseDTO>>
                    {
                        IsSucceeded = false,
                        Message = "RoleId must be greater than zero.",
                        Data = new List<GetTicketTypeRoleResponseDTO>()
                    };
                }

                // 3️⃣ Repository Call
                var ticketTypes = await _unitOfWork.TicketTypeRepository.AllByRoleIdAsync(request.DTO);

                // 4️⃣ Check Result
                if (ticketTypes == null || !ticketTypes.Any())
                {
                    _logger.LogWarning("⚠️ No TicketTypes found for RoleId = {RoleId} ", RoleId);

                    return new ApiResponse<List<GetTicketTypeRoleResponseDTO>>
                    {
                        IsSucceeded = false,
                        Message = "No TicketTypes found for the given module filter.",
                        Data = new List<GetTicketTypeRoleResponseDTO>()
                    };
                }

                // 5️⃣ Mapping
             //   var ticketTypeResponse = _mapper.Map<List<GetTicketTypeResponseDTO>>(ticketTypes);

                _logger.LogInformation("✅ Successfully retrieved {Count} TicketTypes for RoleId = {RoleId} .",
                    ticketTypes.Count, RoleId);

                // 6️⃣ Success Response
                return new ApiResponse<List<GetTicketTypeRoleResponseDTO>>
                {
                    IsSucceeded = true,
                    Message = $"Successfully fetched {ticketTypes.Count} TicketTypes.",
                    Data = ticketTypes
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Exception occurred while fetching TicketTypes by RoleId.");

                return new ApiResponse<List<GetTicketTypeRoleResponseDTO>>
                {
                    IsSucceeded = false,
                    Message = $"An unexpected error occurred while fetching TicketTypes: {ex.Message}",
                    Data = new List<GetTicketTypeRoleResponseDTO>() // Empty list for safety
                };
            }
        }

    }

}
