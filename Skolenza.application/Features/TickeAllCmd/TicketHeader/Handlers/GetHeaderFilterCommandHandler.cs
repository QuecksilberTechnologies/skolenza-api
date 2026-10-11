using AutoMapper;
using skolenza.application.DTOS.TicketDTO.Header;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.TickeAllCmd.TicketHeader.Handlers
{
    public class GetHeaderFilterCommand : IRequest<ApiResponse<List<GetHeaderResponseDTO>>>
    {
        public GetHeaderRequestDTO DTO { get; set; }

        public GetHeaderFilterCommand(GetHeaderRequestDTO dto)
        {
            DTO = dto;
        }
    }
    public class GetHeaderFilterCommandHandler : IRequestHandler<GetHeaderFilterCommand, ApiResponse<List<GetHeaderResponseDTO>>>
    {
        private readonly IMapper _mapper;
        private readonly ITicketHeaderRepository _repository;
        private readonly ILogger<GetHeaderFilterCommandHandler> _logger;

        public GetHeaderFilterCommandHandler(
            IMapper mapper,
            ITicketHeaderRepository repository,
            ILogger<GetHeaderFilterCommandHandler> logger)
        {
            _mapper = mapper;
            _repository = repository;
            _logger = logger;
        }

        public async Task<ApiResponse<List<GetHeaderResponseDTO>>> Handle(GetHeaderFilterCommand request, CancellationToken cancellationToken)
        {
            // ✅ Step 1: Validate request
            if (request == null || request.DTO == null)
            {
                _logger.LogWarning("⚠️ GetHeaderQuery received with null DTO.");
                return new ApiResponse<List<GetHeaderResponseDTO>>
                {
                    IsSucceeded = false,
                    Message = "Invalid request. DTO cannot be null.",
                    Data = null
                };
            }

            try
            {
                _logger.LogInformation("🔍 Fetching headers with filters from request DTO...");

                // ✅ Step 2: Fetch all headers from repository
                var headers = await _repository.GetAllAsync(request.DTO);

                if (headers == null || !headers.Any())
                {
                    _logger.LogInformation("ℹ️ No headers found matching the given filters.");
                    return new ApiResponse<List<GetHeaderResponseDTO>>
                    {
                        IsSucceeded = false,
                        Message = "No headers found for the provided filters.",
                        Data = null
                    };
                }

                // ✅ Step 3: Apply additional filtering if needed
                var filtered = headers.AsQueryable();
                  

                var resultList = filtered.ToList();

                // ✅ Step 4: Logging
                _logger.LogInformation("✅ {Count} headers fetched successfully.", resultList.Count);

                // ✅ Step 5: Return
                return new ApiResponse<List<GetHeaderResponseDTO>>
                {
                    IsSucceeded = true,
                    Message = "Headers fetched successfully.",
                    Data = resultList
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error occurred while fetching headers.");
                return new ApiResponse<List<GetHeaderResponseDTO>>
                {
                    IsSucceeded = false,
                    Message = "An unexpected error occurred while fetching headers.",
                    Data = null
                };
            }
        }
    
    
    }
}
