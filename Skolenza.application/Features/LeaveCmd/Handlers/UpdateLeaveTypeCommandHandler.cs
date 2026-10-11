// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles the request to update a leave type.
// ================================================================

using AutoMapper;
using skolenza.application.DTOs.Leave;
using skolenza.application.Features.LeaveCmd.Commands;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace skolenza.application.Features.LeaveCmd.Commands
{
    #region Command

    /// <summary>
    /// Represents the state-changing request to update a leave type.
    /// </summary>
    public class UpdateLeaveTypeCommand : IRequest<ApiResponse<bool>>
    {
        /// <summary>
        /// Gets or sets the leave-type details to update.
        /// </summary>
        public UpdateLeaveTypeRequestDTO UpdateLeaveTypeDTO { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateLeaveTypeCommand"/> class.
        /// </summary>
        /// <param name="updateLeaveTypeDTO">The leave-type details to update.</param>
        public UpdateLeaveTypeCommand(UpdateLeaveTypeRequestDTO updateLeaveTypeDTO)
        {
            this.UpdateLeaveTypeDTO = updateLeaveTypeDTO;
        }
    }

    #endregion
}

namespace skolenza.application.Features.LeaveCmd.Handlers
{
    #region Handler

    /// <summary>
    /// Handles the request to update a leave type.
    /// </summary>
    public class UpdateTicketTypeCommandHandler : IRequestHandler<UpdateLeaveTypeCommand, ApiResponse<bool>>
    {
        #region Fields

        private readonly ILeaveRepository _leaveRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UpdateTicketTypeCommandHandler> _logger;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateTicketTypeCommandHandler"/> class.
        /// </summary>
        /// <param name="leaveRepository">The repository used to update leave types.</param>
        /// <param name="mapper">The mapper used to convert leave-type data.</param>
        /// <param name="unitOfWork">The unit of work used to commit the update.</param>
        /// <param name="logger">The logger used to record update results.</param>
        public UpdateTicketTypeCommandHandler(
            ILeaveRepository leaveRepository,
            IMapper mapper,
            IUnitOfWork unitOfWork,
            ILogger<UpdateTicketTypeCommandHandler> logger)
        {
            _leaveRepository = leaveRepository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        #endregion

        #region Handler

        /// <summary>
        /// Updates a leave type using the supplied command.
        /// </summary>
        /// <param name="request">The command containing the leave-type details.</param>
        /// <param name="cancellationToken">The token used to observe cancellation.</param>
        /// <returns>A response indicating whether the leave type was updated.</returns>
        public async Task<ApiResponse<bool>> Handle(UpdateLeaveTypeCommand request, CancellationToken cancellationToken)
        {
            bool response = false;
            try
            {
                LeaveType leaveTypeEntity = _mapper.Map<LeaveType>(request.UpdateLeaveTypeDTO);

                response = await _leaveRepository.UpdateLeavTypeAsync(leaveTypeEntity, request.UpdateLeaveTypeDTO.StaffMemberId);

                if (!response)
                {
                    _logger.LogWarning("⚠️ No LeaveType was updated. LeaveTypeId: {LeaveTypeId}, StaffMemberId: {StaffMemberId}",
                        request.UpdateLeaveTypeDTO.Id, request.UpdateLeaveTypeDTO.StaffMemberId);

                    return new ApiResponse<bool>
                    {
                        IsSucceeded = false,
                        Message = "No leave was updated.",
                        Data = false
                    };
                }

                await _unitOfWork.CommitAsync();

                _logger.LogInformation("✅ LeaveType with ID {LeaveTypeId} updated successfully by StaffMemberId {StaffMemberId}.",
                    request.UpdateLeaveTypeDTO.Id, request.UpdateLeaveTypeDTO.StaffMemberId);

                return new ApiResponse<bool>
                {
                    IsSucceeded = true,
                    Message = "Leave updated successfully.",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error occurred while updating LeaveType with ID {LeaveTypeId}.",
                    request.UpdateLeaveTypeDTO.Id);

                return new ApiResponse<bool>
                {
                    IsSucceeded = false,
                    Message = $"An error occurred: {ex.Message}",
                    Data = false
                };
            }
        }

        #endregion
    }

    #endregion
}
