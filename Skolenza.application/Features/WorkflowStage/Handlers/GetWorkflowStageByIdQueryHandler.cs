// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Retrieves a workflow stage by its identifier.
// ================================================================

using skolenza.application.DTOs.WorkflowStage;
using skolenza.application.Features.WorkflowStage.Queries;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace skolenza.application.Features.WorkflowStage.Queries
{
    #region Query

    /// <summary>
    /// Represents the request to retrieve a workflow stage by its identifier.
    /// </summary>
    public class GetWorkflowStageByIdQuery : IRequest<ApiResponse<GetWorkflowStageResponseDTO>>
    {
        /// <summary>
        /// Gets the workflow stage lookup criteria.
        /// </summary>
        public GetWorkflowStageByIdRequestDTO DTO { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GetWorkflowStageByIdQuery"/> class.
        /// </summary>
        /// <param name="dTO">The workflow stage lookup criteria.</param>
        public GetWorkflowStageByIdQuery(GetWorkflowStageByIdRequestDTO dTO)
        {
            this.DTO = dTO;
        }
    }

    #endregion
}

namespace skolenza.application.Features.WorkflowStage.Handlers
{
    /// <summary>
    /// Handles retrieval of a workflow stage by its identifier.
    /// </summary>
    public class GetWorkflowStageByIdQueryHandler : IRequestHandler<GetWorkflowStageByIdQuery, ApiResponse<GetWorkflowStageResponseDTO>>
    {
        #region Fields

        private readonly IWorkflowStagesRepository _workflowRepository;
        private readonly ILogger<GetWorkflowStageByIdQueryHandler> _logger;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="GetWorkflowStageByIdQueryHandler"/> class.
        /// </summary>
        /// <param name="workflowRepository">The repository used to retrieve workflow stages.</param>
        /// <param name="logger">The logger used to record handler activity.</param>
        public GetWorkflowStageByIdQueryHandler(
            IWorkflowStagesRepository workflowRepository,
            ILogger<GetWorkflowStageByIdQueryHandler> logger)
        {
            _workflowRepository = workflowRepository ?? throw new ArgumentNullException(nameof(workflowRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region Handler

        /// <summary>
        /// Retrieves the requested workflow stage.
        /// </summary>
        /// <param name="request">The query containing the workflow stage identifier.</param>
        /// <param name="cancellationToken">A token used to cancel the operation.</param>
        /// <returns>An API response containing the workflow stage when found.</returns>
        public async Task<ApiResponse<GetWorkflowStageResponseDTO>> Handle(
            GetWorkflowStageByIdQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                if (request == null || request.DTO == null || request.DTO.Id <= 0)
                {
                    _logger.LogWarning("Invalid workflow stage Id request received.");

                    return new ApiResponse<GetWorkflowStageResponseDTO>
                    {
                        IsSucceeded = false,
                        Message = "Invalid workflow stage Id.",
                        Data = null
                    };
                }

                var workflowStage = await _workflowRepository.GetByIdAsync(request.DTO.Id);

                if (workflowStage == null)
                {
                    _logger.LogWarning("Workflow stage with Id {Id} was not found.", request.DTO.Id);

                    return new ApiResponse<GetWorkflowStageResponseDTO>
                    {
                        IsSucceeded = false,
                        Message = "Workflow stage not found.",
                        Data = null
                    };
                }

                _logger.LogInformation("Workflow stage with Id {Id} retrieved successfully.", request.DTO.Id);

                return new ApiResponse<GetWorkflowStageResponseDTO>
                {
                    IsSucceeded = true,
                    Message = "Workflow stage fetched successfully.",
                    Data = workflowStage
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching workflow stage with Id {Id}.", request.DTO.Id);

                return new ApiResponse<GetWorkflowStageResponseDTO>
                {
                    IsSucceeded = false,
                    Message = $"Error fetching workflow stage: {ex.Message}",
                    Data = null
                };
            }
        }

        #endregion
    }
}
