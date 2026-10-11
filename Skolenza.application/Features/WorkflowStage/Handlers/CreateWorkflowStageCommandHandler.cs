// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles the request to Create Workflow Stage.
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
using skolenza.application.Features.WorkflowStage.Commands;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.IRepositories;

namespace skolenza.application.Features.WorkflowStage.Commands
{
    #region Command

    /// <summary>
    /// Represents the request to Create Workflow Stage.
    /// </summary>
public class CreateWorkflowStageCommand : IRequest<ApiResponse<List<GetWorkflowStageResponseDTO>>>
    {
        
            public CreateWorkflowStageRequestDTO DTO { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateWorkflowStageCommand"/> class.
        /// </summary>

            public CreateWorkflowStageCommand(CreateWorkflowStageRequestDTO dTO)
            {
                this.DTO = dTO;
            }

        }

    #endregion
}

namespace skolenza.application.Features.WorkflowStage.Handlers
{
/// <summary>
    /// Handles the creation of workflow stages.
    /// </summary>
    public class CreateWorkflowStageCommandHandler: IRequestHandler<CreateWorkflowStageCommand, ApiResponse<List<GetWorkflowStageResponseDTO>>>
    {
        #region Fields

        private readonly IWorkflowStagesRepository _repository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStoreProcedureRepository _commonRepository;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateWorkflowStageCommandHandler"/> class.
        /// </summary>


        public CreateWorkflowStageCommandHandler(
            IWorkflowStagesRepository repository,
            IMapper mapper,
            IUnitOfWork unitOfWork,
            IStoreProcedureRepository commonRepository)
        {
            _repository = repository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _commonRepository = commonRepository;
        }
        #endregion

        #region Handler
        /// <summary>
        /// Processes the supplied CreateWorkflowStageCommand.
        /// </summary>
        /// <param name="request">The request to process.</param>
        /// <param name="cancellationToken">The token used to observe cancellation.</param>
        /// <returns>The response produced for the request.</returns>


        public async Task<ApiResponse<List<GetWorkflowStageResponseDTO>>> Handle(CreateWorkflowStageCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1️⃣ Validate incoming request
                if (request.DTO == null)
                {
                    return new ApiResponse<List<GetWorkflowStageResponseDTO>>
                    {
                        IsSucceeded = false,
                        Message = "Invalid workflow stage request. Data is required.",
                        Data = new List<GetWorkflowStageResponseDTO>()
                    };
                }

          

                // 3️⃣ Repository Call — Add workflow stage
                var response = await _unitOfWork.WorkflowStagesRepository.AddAsync(request.DTO);

                if (response == null || !response.Any())
                {
                    return new ApiResponse<List<GetWorkflowStageResponseDTO>>
                    {
                        IsSucceeded = false,
                        Message = "Workflow stage creation failed.",
                        Data = new List<GetWorkflowStageResponseDTO>()
                    };
                }

                // 4️⃣ Success response
                return new ApiResponse<List<GetWorkflowStageResponseDTO>>
                {
                    IsSucceeded = true,
                    Message = "Workflow stage created successfully.",
                    Data = response
                };
            }
            catch (Exception ex)
            {
                // 5️⃣ Exception handling with clear message
                return new ApiResponse<List<GetWorkflowStageResponseDTO>>
                {
                    IsSucceeded = false,
                    Message = $"An error occurred while creating workflow stage: {ex.Message}",
                    Data = new List<GetWorkflowStageResponseDTO>()
                };
            }
        }
    
        #endregion
}
}
