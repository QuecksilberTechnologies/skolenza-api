using AutoMapper;
using skolenza.application.DTOS.TicketDTO.Header;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using skolenza.domain.Entity;
using MediatR;

namespace skolenza.application.Features.TickeAllCmd.TicketHeader.Handlers
{
    public class GetHeaderByIdQuery : IRequest<ApiResponse<List<GetHeaderResponseDTO>>>
    {
        public GetHeaderRequestDTO DTO { get; set; }

        public GetHeaderByIdQuery(GetHeaderRequestDTO dto)
        {
            DTO = dto;
        }
    }
    public class GetHeaderByIdsQueryHandler : IRequestHandler<GetHeaderByIdQuery, ApiResponse<List<GetHeaderResponseDTO>>>
    {
        private readonly IMapper _mapper;
        private readonly ITicketHeaderRepository _repository;
        private readonly ILogger<GetHeaderByIdsQueryHandler> _logger;

        public GetHeaderByIdsQueryHandler(
            IMapper mapper,
            ITicketHeaderRepository repository,
            ILogger<GetHeaderByIdsQueryHandler> logger)
        {
            _mapper = mapper;
            _repository = repository;
            _logger = logger;
        }

        public async Task<ApiResponse<List<GetHeaderResponseDTO>>> Handle(GetHeaderByIdQuery request, CancellationToken cancellationToken)
        {
            // ✅ Step 1: Validate request
            if (request == null || request.DTO == null)
            {
                _logger.LogWarning("⚠️ GetHeaderByIdQuery received with null DTO.");
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
                var headers = await _repository.GetAllHeaderAsync(request.DTO);

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

                if (!string.IsNullOrWhiteSpace(request.DTO.HeaderName))
                    filtered = filtered.Where(h => h.HeaderName.Contains(request.DTO.HeaderName, StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrWhiteSpace(request.DTO.Description))
                    filtered = filtered.Where(h => h.Description.Contains(request.DTO.Description, StringComparison.OrdinalIgnoreCase));

                if (request.DTO.IsActive.HasValue)
                    filtered = filtered.Where(h => h.IsActive == request.DTO.IsActive.Value);

                //if (request.DTO.TicketClassificationId.HasValue)
                //    filtered = filtered.Where(h => h.TicketClassificationId == request.DTO.TicketClassificationId.Value);

                //if (request.DTO.IsAssetRelated.HasValue)
                //    filtered = filtered.Where(h => h.IsAssetRelated == request.DTO.IsAssetRelated.Value);

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
