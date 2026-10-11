// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles the request to create a leave type.
// ================================================================

using AutoMapper;
using skolenza.application.Constants;
using skolenza.application.DTOs.Leave;
using skolenza.application.Features.LeaveCmd.Commands;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace skolenza.application.Features.LeaveCmd.Commands
{
    #region Command

    /// <summary>
    /// Represents the state-changing request to create a leave type.
    /// </summary>
    public class CreateLeaveTypeCommand : IRequest<ApiResponse<List<GetLeaveTypResponseDTO>>>
    {
        /// <summary>
        /// Gets or sets the leave-type details to create.
        /// </summary>
        public CreateLeaveTypeRequestDTO createLeaveTypeDTO { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateLeaveTypeCommand"/> class.
        /// </summary>
        /// <param name="createLeaveTypeDTO">The leave-type details to create.</param>
        public CreateLeaveTypeCommand(CreateLeaveTypeRequestDTO createLeaveTypeDTO)
        {
            this.createLeaveTypeDTO = createLeaveTypeDTO;
        }
    }

    #endregion
}

namespace skolenza.application.Features.LeaveCmd.Handlers
{
    #region Handler

    /// <summary>
    /// Handles the request to create a leave type.
    /// </summary>
    public class CreateLeaveTypeCommandHandler : IRequestHandler<CreateLeaveTypeCommand, ApiResponse<List<GetLeaveTypResponseDTO>>>
    {
        #region Fields

        private readonly ILeaveRepository _leaveRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStoreProcedureRepository _commonRepository;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateLeaveTypeCommandHandler"/> class.
        /// </summary>
        /// <param name="leaveRepository">The repository used to create leave types.</param>
        /// <param name="mapper">The mapper used to convert leave-type data.</param>
        /// <param name="unitOfWork">The unit of work supplied to this handler.</param>
        /// <param name="commonRepository">The stored-procedure repository supplied to this handler.</param>
        public CreateLeaveTypeCommandHandler(ILeaveRepository leaveRepository, IMapper mapper, IUnitOfWork unitOfWork, IStoreProcedureRepository commonRepository)
        {
            _leaveRepository = leaveRepository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _commonRepository = commonRepository;
        }

        #endregion

        #region Handler

        /// <summary>
        /// Creates a leave type using the supplied command.
        /// </summary>
        /// <param name="request">The command containing the leave-type details.</param>
        /// <param name="cancellationToken">The token used to observe cancellation.</param>
        /// <returns>A response containing the created leave types.</returns>
        public async Task<ApiResponse<List<GetLeaveTypResponseDTO>>> Handle(CreateLeaveTypeCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1️⃣ Validation
                if (request.createLeaveTypeDTO == null)
                {
                    return new ApiResponse<List<GetLeaveTypResponseDTO>>
                    {
                        IsSucceeded = false,
                        Message = "Invalid request. LeaveType data is required.",
                        Data = new List<GetLeaveTypResponseDTO>()
                    };
                }

                if (string.IsNullOrWhiteSpace(request.createLeaveTypeDTO.LeaveName.Trim()))
                {
                    return new ApiResponse<List<GetLeaveTypResponseDTO>>
                    {
                        IsSucceeded = false,
                        Message = "Leave Name is required.",
                        Data = new List<GetLeaveTypResponseDTO>()
                    };
                }

                if (request.createLeaveTypeDTO.InstitutionId <= 0)
                {
                    return new ApiResponse<List<GetLeaveTypResponseDTO>>
                    {
                        IsSucceeded = false,
                        Message = "InstitutionId must be valid.",
                        Data = new List<GetLeaveTypResponseDTO>()
                    };
                }

                // 2️⃣ DTO → Entity mapping
                LeaveType leaveTypeEntity = _mapper.Map<LeaveType>(request.createLeaveTypeDTO);
                leaveTypeEntity.AddedById = request.createLeaveTypeDTO.StaffMemberId;
                leaveTypeEntity.AddedDateTime = DateTime.UtcNow;
                leaveTypeEntity.IsActive = ConstantValues.IsByDefaultTrue;
                // 3️⃣ Repository Call
                List<LeaveType> leaveTypes = await _leaveRepository.CreateLeaveTypeAsync(leaveTypeEntity);
                if (leaveTypes == null || !leaveTypes.Any())
                {
                    return new ApiResponse<List<GetLeaveTypResponseDTO>>
                    {
                        IsSucceeded = false,
                        Message = "LeaveType creation failed.",
                        Data = new List<GetLeaveTypResponseDTO>()
                    };
                }

                // 4️⃣ Mapping Entity → DTO
                List<GetLeaveTypResponseDTO> leaveTypeDTOs = _mapper.Map<List<GetLeaveTypResponseDTO>>(leaveTypes);

                return new ApiResponse<List<GetLeaveTypResponseDTO>>
                {
                    IsSucceeded = true,
                    Message = "LeaveType created successfully.",
                    Data = leaveTypeDTOs
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<GetLeaveTypResponseDTO>>
                {
                    IsSucceeded = false,
                    Message = $"An error occurred while creating LeaveType: {ex.Message}",
                    Data = new List<GetLeaveTypResponseDTO>()
                };
            }
        }

        #endregion
    }

    #endregion
}
