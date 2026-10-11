// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles the request to Create Operation.
// ================================================================

using skolenza.application.DTOs.Operation;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using skolenza.application.Features.LeaveCmd.Commands;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Interfaces;
using skolenza.application.Features.OperationCmd.Commands;

namespace skolenza.application.Features.OperationCmd.Commands
{
    #region Command

    /// <summary>
    /// Represents the request to Create Operation.
    /// </summary>
public class CreateOperationCommand : IRequest<ApiResponse<List<GetOperationResponseDTO>>>
    {

        public CreateOperationRequestDTO createOperationDTO { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateOperationCommand"/> class.
        /// </summary>

        public CreateOperationCommand(CreateOperationRequestDTO createOperationDTO)
        {
            this.createOperationDTO = createOperationDTO;
        }

    }

    #endregion
}

namespace skolenza.application.Features.OperationCmd.Handlers
{
    /// <summary>
    /// Handles the request to Create Operation.
    /// </summary>
public class CreateOperationCommandHandler :  IRequestHandler<CreateOperationCommand, ApiResponse<List<GetOperationResponseDTO>>>
    {
        #region Fields

        private readonly IOperationRepository _operationRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateOperationCommandHandler"/> class.
        /// </summary>


        public CreateOperationCommandHandler(IOperationRepository operationRepository, IMapper mapper, IUnitOfWork unitOfWork)
        {
            _operationRepository = operationRepository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }
        #endregion

        #region Handler
        /// <summary>
        /// Processes the supplied CreateOperationCommand.
        /// </summary>
        /// <param name="request">The request to process.</param>
        /// <param name="cancellationToken">The token used to observe cancellation.</param>
        /// <returns>The response produced for the request.</returns>

        public async Task<ApiResponse<List<GetOperationResponseDTO>>> Handle(CreateOperationCommand request, CancellationToken cancellationToken)
        {
            try
            {
                Operation operationEntity = _mapper.Map<Operation>(request.createOperationDTO);

                List<Operation> operations = await _operationRepository.CreateOperationAsync(operationEntity);

                if (operations == null || !operations.Any())
                {
                    return new ApiResponse<List<GetOperationResponseDTO>>
                    {
                        IsSucceeded = false,
                        Message = "No Operation were created.",
                        Data = new List<GetOperationResponseDTO>()
                    };
                }

                List<GetOperationResponseDTO> operationDTOs = _mapper.Map<List<GetOperationResponseDTO>>(operations);

                return new ApiResponse<List<GetOperationResponseDTO>>
                {
                    IsSucceeded = true,
                    Message = " Operation created successfully",
                    Data = operationDTOs
                };
            }
            catch (Exception ex)
            {
                //  _logger.LogError(ex, "Error occurred while creating role.");
                return new ApiResponse<List<GetOperationResponseDTO>>

                {
                    IsSucceeded = false,
                    Message = $"An error occurred: {ex.Message}",
                    Data = null
                };
            }
        }


    
        #endregion
}
}
