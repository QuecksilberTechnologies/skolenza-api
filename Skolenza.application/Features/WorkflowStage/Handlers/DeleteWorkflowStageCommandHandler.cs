// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles requests to delete workflow stages.
// ================================================================

using skolenza.application.Constants;
using skolenza.application.DTOs.WorkflowStage;
using skolenza.application.Exceptions;
using skolenza.application.Features.WorkflowStage.Commands;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.WorkflowStage.Commands
{
    #region Command

    /// <summary>
    /// Represents a request to delete a workflow stage.
    /// </summary>
    public class DeleteWorkflowStageCommand : IRequest<ApiResponse<bool>>
    {
        public DeleteWorkflowStageRequestDTO? DTO { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteWorkflowStageCommand"/> class.
        /// </summary>
        public DeleteWorkflowStageCommand(DeleteWorkflowStageRequestDTO dto)
        {
            DTO = dto;
        }
    }

    #endregion
}

namespace skolenza.application.Features.WorkflowStage.Handlers
{
    #region Handler

    /// <summary>
    /// Handles requests to delete workflow stages.
    /// </summary>
    public class DeleteWorkflowStageCommandHandler : IRequestHandler<DeleteWorkflowStageCommand, ApiResponse<bool>>
    {
        #region Fields

        private readonly IWorkflowStagesRepository _workflowRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeleteWorkflowStageCommandHandler> _logger;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteWorkflowStageCommandHandler"/> class.
        /// </summary>
        public DeleteWorkflowStageCommandHandler(
            IWorkflowStagesRepository workflowRepository,
            IUnitOfWork unitOfWork,
            ILogger<DeleteWorkflowStageCommandHandler> logger)
        {
            _workflowRepository = workflowRepository ?? throw new ArgumentNullException(nameof(workflowRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region Handle

        /// <summary>
        /// Deletes the requested workflow stage and constructs the successful API response.
        /// </summary>
        public async Task<ApiResponse<bool>> Handle(
            DeleteWorkflowStageCommand request,
            CancellationToken cancellationToken)
        {
            if (request.DTO == null || request.DTO.Id <= 0)
            {
                throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
            }

            var entity = await _workflowRepository.GetByIdAsync(request.DTO.Id);
            if (entity == null)
            {
                throw new NotFoundException(AppConstants.ErrorMessages.ResourceNotFound);
            }

            await _workflowRepository.DeleteAsync(request.DTO.Id, request.DTO.StaffMemberId);
            await _unitOfWork.CommitAsync();

            _logger.LogInformation(
                "Workflow stage {WorkflowStageId} was deleted by staffMember {StaffMemberId}.",
                request.DTO.Id,
                request.DTO.StaffMemberId);

            return ApiResponse<bool>.Success(true, AppConstants.SuccessMessages.WorkflowStageDeleted);
        }

        #endregion
    }

    #endregion
}
