// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Processes the GetAllWorkflowStageQuery use case.
// ================================================================

using skolenza.application.DTOs.Leave;
using skolenza.application.DTOs.WorkflowStage;
using skolenza.application.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AutoMapper;
using skolenza.application.Features.WorkflowStage.Queries;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.IRepositories;
using Microsoft.Extensions.Logging;
using System.Threading;

using MediatR;

namespace skolenza.application.Features.WorkflowStage.Queries
{
    #region Query

    /// <summary>
    /// Represents the query request for Get All Workflow Stage.
    /// </summary>
public class GetAllWorkflowStageQuery : IRequest<ApiResponse<List<GetWorkflowStageResponseDTO>>>
    {
        public GetWorkflowStageRequestDTO DTO { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="GetAllWorkflowStageQuery"/> class.
        /// </summary>

    public GetAllWorkflowStageQuery(GetWorkflowStageRequestDTO dTO)
    {
        this.DTO = dTO;
    }
}

    #endregion
}

namespace skolenza.application.Features.WorkflowStage.Handlers
{
/// <summary>
    /// Handles query to fetch all workflow stages.
    /// </summary>
    public class GetAllWorkflowStageQueryHandler : IRequestHandler<GetAllWorkflowStageQuery, ApiResponse<List<GetWorkflowStageResponseDTO>>>
    {
        #region Fields

        private readonly IWorkflowStagesRepository _workflowRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStoreProcedureRepository _commonRepository;
        private readonly ILogger<GetAllWorkflowStageQueryHandler> _logger;
        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="GetAllWorkflowStageQueryHandler"/> class.
        /// </summary>

        public GetAllWorkflowStageQueryHandler(
            IWorkflowStagesRepository workflowRepository,
            IMapper mapper,
            IUnitOfWork unitOfWork,
            IStoreProcedureRepository commonRepository,
            ILogger<GetAllWorkflowStageQueryHandler> logger)
        {
            _workflowRepository = workflowRepository ?? throw new ArgumentNullException(nameof(workflowRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _commonRepository = commonRepository ?? throw new ArgumentNullException(nameof(commonRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region Handler

        /// <summary>
        /// Handles the retrieval of all workflow stages based on filters.
        /// </summary>
        /// <param name="request">The query request to process.</param>
        /// <param name="cancellationToken">A token used to cancel the operation.</param>
        /// <returns>The response produced by handling the query.</returns>
        public async Task<ApiResponse<List<GetWorkflowStageResponseDTO>>> Handle(GetAllWorkflowStageQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // 1️⃣ Validation
                if (request == null || request.DTO == null)
                {
                    _logger.LogWarning("⚠️ Invalid request received in GetAllWorkflowStageQuery.");
                    return new ApiResponse<List<GetWorkflowStageResponseDTO>>
                    {
                        IsSucceeded = false,
                        Message = "Invalid request. Please provide valid filter data.",
                        Data = new List<GetWorkflowStageResponseDTO>()
                    };
                }
 

                // 2️⃣ Repository Call
                var workflowStages = await _workflowRepository.AllAsync(request.DTO);

                // 3️⃣ Result Validation
                if (workflowStages == null || !workflowStages.Any())
                {
                   
                    return new ApiResponse<List<GetWorkflowStageResponseDTO>>
                    {
                        IsSucceeded = false,
                        Message = "No workflow stages found.",
                        Data = new List<GetWorkflowStageResponseDTO>()
                    };
                }

             

                return new ApiResponse<List<GetWorkflowStageResponseDTO>>
                {
                    IsSucceeded = true,
                    Message = "Workflow stages fetched successfully.",
                    Data = workflowStages
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error occurred while fetching workflow stages.");
                return new ApiResponse<List<GetWorkflowStageResponseDTO>>
                {
                    IsSucceeded = false,
                    Message = $"An error occurred while fetching workflow stages: {ex.Message}",
                    Data = new List<GetWorkflowStageResponseDTO>()
                };
            }
        }
    
        #endregion
}
}
