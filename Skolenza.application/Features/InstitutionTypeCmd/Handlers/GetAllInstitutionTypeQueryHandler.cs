using AutoMapper;
using skolenza.application.DTOs.SubscriptionModule;
using skolenza.application.DTOs.InstitutionType;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Wrappers;
using skolenza.domain.Entity; using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.Features.InstitutionTypeCmd.Handlers
{
    public class GetAllInstitutionTypeQuery : IRequest<ApiResponse<List<InstitutionTypeResponseDTO>>>
    {
        public int PlanId { get; set; }

        public GetAllInstitutionTypeQuery(int planId)
        {
            PlanId = planId;
        }
    }

    public class GetAllInstitutionTypeQueryHandler
    : IRequestHandler<GetAllInstitutionTypeQuery, ApiResponse<List<InstitutionTypeResponseDTO>>>
    {
        private readonly IInstitutionTypeRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<GetAllInstitutionTypeQueryHandler> _logger;

        public GetAllInstitutionTypeQueryHandler(
            IInstitutionTypeRepository repository,
            IMapper mapper,
            ILogger<GetAllInstitutionTypeQueryHandler> logger)
        {
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ApiResponse<List<InstitutionTypeResponseDTO>>> Handle(GetAllInstitutionTypeQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Fetching Institution types. PlanId: {PlanId}", request.PlanId);

                var institutionTypes = await _repository.GetAllActiveInstitutionTypesAsync();

                var institutionTypeList = _mapper.Map<List<InstitutionTypeResponseDTO>>(institutionTypes);

                return new ApiResponse<List<InstitutionTypeResponseDTO>>
                {
                    IsSucceeded = true,
                    Message = "Institution types fetched successfully.",
                    Data = institutionTypeList
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Institution types.");

                return new ApiResponse<List<InstitutionTypeResponseDTO>>
                {
                    IsSucceeded = false,
                    Message = "Failed to fetch Institution types.",
                    Errors = new List<string> { ex.Message },
                    Data = null
                };
            }
        }
    }
}
