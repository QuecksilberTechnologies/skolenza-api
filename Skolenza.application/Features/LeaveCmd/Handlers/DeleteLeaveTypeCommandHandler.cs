// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles the request to delete a leave type.
// ================================================================

using skolenza.application.Constants;
using skolenza.application.DTOs.Leave;
using skolenza.application.Features.LeaveCmd.Commands;
using skolenza.application.Interfaces;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace skolenza.application.Features.LeaveCmd.Commands
{
    #region Command

    /// <summary>
    /// Represents the state-changing request to delete a leave type.
    /// </summary>
    public class DeleteLeaveTypeCommand : IRequest<ApiResponse<bool>>
    {
        /// <summary>
        /// Gets or sets the leave-type details used to delete the leave type.
        /// </summary>
        public DeleteLeaveRequestDTO DTO { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteLeaveTypeCommand"/> class.
        /// </summary>
        /// <param name="dTO">The leave-type details used to delete the leave type.</param>
        public DeleteLeaveTypeCommand(DeleteLeaveRequestDTO dTO)
        {
            DTO = dTO;
        }
    }

    #endregion
}

namespace skolenza.application.Features.LeaveCmd.Handlers
{
    #region Handler

    /// <summary>
    /// Handles the request to delete a leave type.
    /// </summary>
    public class DeleteLeaveTypeCommandHandler : IRequestHandler<DeleteLeaveTypeCommand, ApiResponse<bool>>
    {
        #region Fields

        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeleteLeaveTypeCommandHandler> _logger;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteLeaveTypeCommandHandler"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work used to delete the leave type.</param>
        /// <param name="logger">The logger used to record deletion results.</param>
        public DeleteLeaveTypeCommandHandler(IUnitOfWork unitOfWork, ILogger<DeleteLeaveTypeCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        #endregion

        #region Handler

        /// <summary>
        /// Deletes a leave type using the supplied command.
        /// </summary>
        /// <param name="request">The command containing the leave-type details.</param>
        /// <param name="cancellationToken">The token used to observe cancellation.</param>
        /// <returns>A response indicating whether the leave type was deleted.</returns>
        public async Task<ApiResponse<bool>> Handle(DeleteLeaveTypeCommand request, CancellationToken cancellationToken)
        {
            try
            {
                if (request.DTO.Id <= 0)
                {
                    return new ApiResponse<bool>
                    {
                        IsSucceeded = false,
                        Message = "Invalid LeaveType Id.",
                        Data = false
                    };
                }

                // Repository se entity fetch karo
                var leaveType = await _unitOfWork.LeaveRepository.GetLeaveByIdAsync(request.DTO.Id);

                if (leaveType == null)
                {
                    return new ApiResponse<bool>
                    {
                        IsSucceeded = false,
                        Message = "LeaveType not found.",
                        Data = false
                    };
                }

                // Soft delete
                leaveType.IsSoftDeleted = ConstantValues.IsByDefaultTrue;
                leaveType.SoftDeletedBy = request.DTO.StaffMemberId; // Yaha aap userId pass kar sakte ho
                leaveType.SoftDeletedDateTime = DateTime.UtcNow;
                leaveType.IsActive = ConstantValues.IsByDefaultFalse; ;
                leaveType.IsSoftDeleted = ConstantValues.IsByDefaultTrue; ;

                await _unitOfWork.LeaveRepository.DeleteLeaveAsync(leaveType);

                await _unitOfWork.CommitAsync();

                _logger.LogInformation("LeaveType with Id {Id} deleted successfully.", request.DTO.Id);

                return new ApiResponse<bool>
                {
                    IsSucceeded = true,
                    Message = "LeaveType deleted successfully.",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting LeaveType Id {Id}", request.DTO.Id);
                return new ApiResponse<bool>
                {
                    IsSucceeded = false,
                    Message = $"An error occurred while deleting LeaveType: {ex.Message}",
                    Data = false
                };
            }
        }

        #endregion
    }

    #endregion
}
