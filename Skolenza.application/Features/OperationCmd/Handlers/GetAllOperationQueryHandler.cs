// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Processes the GetAllOperationCommand use case.
// ================================================================

using skolenza.application.DTOs.Operation;
using skolenza.application.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AutoMapper;
using skolenza.application.Features.OperationCmd.Commands;
using skolenza.application.Features.TransportCmd.Handlers;
using skolenza.application.Features.TransportCmd.Queries;
using skolenza.application.Interfaces;
using skolenza.domain.Entity;
using Microsoft.Extensions.Logging;

using MediatR;

namespace skolenza.application.Features.OperationCmd.Commands
{
    #region Command

    /// <summary>
    /// Represents the command request for Get All Operation.
    /// </summary>
public class GetAllOperationCommand : IRequest<ApiResponse<List<GetOperationResponseDTO>>>
    {
        public GetOperationRequestDTO? Dto { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="GetAllOperationCommand"/> class.
        /// </summary>

        public GetAllOperationCommand(GetOperationRequestDTO dto)
        {
            this.Dto = dto;
        }
    }

    #endregion
}

namespace skolenza.application.Features.OperationCmd.Handlers
{
    /// <summary>
    /// Handles the request for Get All Operation.
    /// </summary>
public class GetAllOperationQueryHandler : IRequestHandler<GetAllOperationCommand, ApiResponse<List<GetOperationResponseDTO>>>
    {
        #region Fields

        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetAllOperationQueryHandler> _logger;
        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="GetAllOperationQueryHandler"/> class.
        /// </summary>

        public GetAllOperationQueryHandler(IMapper mapper, IUnitOfWork unitOfWork, ILogger<GetAllOperationQueryHandler> logger)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }
        #endregion

        #region Handler

        /// <summary>
        /// Handles the request asynchronously.
        /// </summary>
        /// <param name="request">The request to process.</param>
        /// <param name="cancellationToken">A token used to cancel the operation.</param>
        /// <returns>The response produced by handling the request.</returns>
        public async Task<ApiResponse<List<GetOperationResponseDTO>>> Handle(GetAllOperationCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // ✅ Correcting the method call
                
                List<Operation> operationDTOs = await _unitOfWork.OperationRepository.GetAllOperationAsync();

                //if (roles == null || !roles.Any())
                //{
                //    _logger.LogWarning("No Operations found.");
                //    return new ApiResponse<List<GetAllRoleDTO>>(false, "No Operations found", new List<GetAllRoleDTO>());
                //}

                //// ✅ Map Role entities to DTOs
                var getAllOperationDTOs = _mapper.Map<List<GetOperationResponseDTO>>(operationDTOs);
                 
                _logger.LogInformation("Successfully retrieved {Count} Operations.", getAllOperationDTOs.Count);
                return new ApiResponse<List<GetOperationResponseDTO>>
                {
                    IsSucceeded = true,
                    Message = "Operations fetched successfully.",
                    Data = getAllOperationDTOs
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching Operations.");
                return new ApiResponse<List<GetOperationResponseDTO>>
                {
                    IsSucceeded = false,
                    Message = "Operations fetched successfully.",
                    Data = null
                };
            }
        }



    
        #endregion
}
}
