// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles the request to Update Workflow Stage.
// ================================================================

using skolenza.application.DTOs.Leave;
using skolenza.application.DTOs.WorkflowStage;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using skolenza.application.Constants;
using skolenza.application.Features.WorkflowStage.Commands;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.IRepositories;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace skolenza.application.Features.WorkflowStage.Commands
{
    #region Command

    /// <summary>
    /// Represents the request to Update Workflow Stage.
    /// </summary>
public class UpdateWorkflowStageCommand : IRequest<ApiResponse<bool>>
    {
        
            public UpdateWorkflowStageRequestDTO DTO { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateWorkflowStageCommand"/> class.
        /// </summary>

    public UpdateWorkflowStageCommand(UpdateWorkflowStageRequestDTO dTO)
    {
        this.DTO = dTO;
    }

}

    #endregion
}

namespace skolenza.application.Features.WorkflowStage.Handlers
{
    /// <summary>
    /// Handles the request to Update Workflow Stage.
    /// </summary>
public class UpdateWorkflowStageCommandHandler : IRequestHandler<UpdateWorkflowStageCommand, ApiResponse<bool>>
    {
        #region Fields

        private readonly IWorkflowStagesRepository _repository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStoreProcedureRepository _commonRepository;
        private readonly ILogger<UpdateWorkflowStageCommandHandler> _logger;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateWorkflowStageCommandHandler"/> class.
        /// </summary>


        public UpdateWorkflowStageCommandHandler(
            IWorkflowStagesRepository repository,
            IMapper mapper,
            IUnitOfWork unitOfWork,
            IStoreProcedureRepository commonRepository,
            ILogger<UpdateWorkflowStageCommandHandler> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _commonRepository = commonRepository ?? throw new ArgumentNullException(nameof(commonRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        #endregion

        #region Handler
        /// <summary>
        /// Processes the supplied UpdateWorkflowStageCommand.
        /// </summary>
        /// <param name="request">The request to process.</param>
        /// <param name="cancellationToken">The token used to observe cancellation.</param>
        /// <returns>The response produced for the request.</returns>


        public async Task<ApiResponse<bool>> Handle(UpdateWorkflowStageCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1️⃣ Validate Request
                if (request.DTO == null)
                {
                    return new ApiResponse<bool>
                    {
                        IsSucceeded = false,
                        Message = "Invalid request. WorkflowStage data is required.",
                        Data = false
                    };
                }

                if (request.DTO.Id <= 0)
                {
                    return new ApiResponse<bool>
                    {
                        IsSucceeded = false,
                        Message = "WorkflowStage Id must be valid.",
                        Data = false
                    };
                }

                // 2️⃣ Update WorkflowStage using Repository
                bool isUpdated = await _repository.UpdateAsync(request.DTO);

                if (!isUpdated)
                {
                    return new ApiResponse<bool>
                    {
                        IsSucceeded = false,
                        Message = "WorkflowStage update failed. Either not found or no changes detected.",
                        Data = false
                    };
                }

                // 3️⃣ Commit Transaction
                await _unitOfWork.CommitAsync();

                _logger.LogInformation("WorkflowStage updated successfully with Id {Id}", request.DTO.Id);

                // 4️⃣ Return Success Response
                return new ApiResponse<bool>
                {
                    IsSucceeded = true,
                    Message = "WorkflowStage updated successfully.",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating WorkflowStage with Id {Id}", request.DTO?.Id);

                return new ApiResponse<bool>
                {
                    IsSucceeded = false,
                    Message = $"An error occurred while updating WorkflowStage: {ex.Message}",
                    Data = false
                };
            }
        }
    
        #endregion
}
}
