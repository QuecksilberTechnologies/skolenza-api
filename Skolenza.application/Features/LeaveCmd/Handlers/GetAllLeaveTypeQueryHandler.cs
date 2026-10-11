// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles the read-only request to retrieve all leave types.
// ================================================================

using AutoMapper;
using skolenza.application.DTOs.Leave;
using skolenza.application.Features.LeaveCmd.Queries;
using skolenza.application.Features.RoleCmd.Handlers;
using skolenza.application.Interfaces;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace skolenza.application.Features.LeaveCmd.Queries
{
    #region Query

    /// <summary>
    /// Represents the read-only request to retrieve leave types matching the supplied criteria.
    /// </summary>
    public class GetAllLeaveTypeQuery : IRequest<ApiResponse<List<GetLeaveTypResponseDTO>>>
    {
        /// <summary>
        /// Gets or sets the criteria used to retrieve leave types.
        /// </summary>
        public GetLeaveTypeRequestDTO DTO { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GetAllLeaveTypeQuery"/> class.
        /// </summary>
        /// <param name="dto">The criteria used to retrieve leave types.</param>
        public GetAllLeaveTypeQuery(GetLeaveTypeRequestDTO dto)
        {
            this.DTO = dto;
        }
    }

    #endregion
}

namespace skolenza.application.Features.LeaveCmd.Handlers
{
    #region Handler

    /// <summary>
    /// Handles the read-only request to retrieve all leave types.
    /// </summary>
    public class GetAllLeaveTypeQueryHandler : IRequestHandler<GetAllLeaveTypeQuery, ApiResponse<List<GetLeaveTypResponseDTO>>>
    {
        #region Fields

        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetAllLeaveTypeQueryHandler> _logger;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="GetAllLeaveTypeQueryHandler"/> class.
        /// </summary>
        /// <param name="mapper">The mapper used to convert leave-type data.</param>
        /// <param name="unitOfWork">The unit of work used to retrieve leave types.</param>
        /// <param name="logger">The logger used to record query results.</param>
        public GetAllLeaveTypeQueryHandler(IMapper mapper, IUnitOfWork unitOfWork, ILogger<GetAllLeaveTypeQueryHandler> logger)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        #endregion

        #region Handler

        /// <summary>
        /// Retrieves leave types using the supplied query.
        /// </summary>
        /// <param name="request">The query containing the leave-type criteria.</param>
        /// <param name="cancellationToken">The token used to observe cancellation.</param>
        /// <returns>A response containing the matching leave types.</returns>
        public async Task<ApiResponse<List<GetLeaveTypResponseDTO>>> Handle(GetAllLeaveTypeQuery request, CancellationToken cancellationToken)
        {
            try
            {
                bool?    IsActive = request.DTO.IsActive;
                if (IsActive == null)
                {
                    IsActive=false;
                }
                // 🔹 Repository se data fetch karo
                List<LeaveType> leaveTypes = await _unitOfWork.LeaveRepository.GetAllLeaveAsync(IsActive, request.DTO.InstitutionId);

                // 🔹 Validation: Agar list null ya empty hai
                if (leaveTypes == null || !leaveTypes.Any())
                {
                    _logger.LogWarning("⚠️ No LeaveTypes found in database.");

                    return new ApiResponse<List<GetLeaveTypResponseDTO>>
                    {
                        IsSucceeded = false,
                        Message = "No LeaveTypes found.",
                        Data = new List<GetLeaveTypResponseDTO>()
                    };
                }

                // 🔹 Map entity -> DTO
                var leaveTypeDTOs = _mapper.Map<List<GetLeaveTypResponseDTO>>(leaveTypes);

                _logger.LogInformation("✅ Successfully retrieved {Count} LeaveTypes.", leaveTypeDTOs.Count);

                return new ApiResponse<List<GetLeaveTypResponseDTO>>
                {
                    IsSucceeded = true,
                    Message = "LeaveTypes fetched successfully.",
                    Data = leaveTypeDTOs
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error occurred while fetching LeaveTypes.");

                return new ApiResponse<List<GetLeaveTypResponseDTO>>
                {
                    IsSucceeded = false,
                    Message = $"An error occurred while fetching LeaveTypes: {ex.Message}",
                    Data = new List<GetLeaveTypResponseDTO>() // null ke bajaye empty list bhejna better hai
                };
            }
        }

        #endregion
    }

    #endregion
}
