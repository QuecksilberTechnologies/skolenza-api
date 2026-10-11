// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles the read-only request to retrieve Get Page Operation Permission.
// ================================================================

using skolenza.application.DTOs.Operation;
using skolenza.application.DTOs.Role;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using skolenza.application.Features.OperationCmd.Queries;
using skolenza.application.Interfaces;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace skolenza.application.Features.OperationCmd.Queries
{
    #region Query

    /// <summary>
    /// Represents the read-only request to retrieve Get Page Operation Permission.
    /// </summary>
public class GetPageOperationPermissionQuery : IRequest<ApiResponse<GetHasAccessOperationDTO>>
    {
        public GetCheckOperationPermissionRequestDTO? CheckOperationPermissionRequest { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="GetPageOperationPermissionQuery"/> class.
        /// </summary>
        
        public GetPageOperationPermissionQuery(GetCheckOperationPermissionRequestDTO checkOperationPermissionRequest)
        {
            this.CheckOperationPermissionRequest = checkOperationPermissionRequest;
        }
    }

    #endregion
}

namespace skolenza.application.Features.OperationCmd.Handlers
{
    /// <summary>
    /// Handles the request to Get Page Operation Permission.
    /// </summary>
public class GetPageOperationPermissionQueryHandler : IRequestHandler<GetPageOperationPermissionQuery, ApiResponse<GetHasAccessOperationDTO>>
    {
        #region Fields

        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetPageOperationPermissionQueryHandler> _logger;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="GetPageOperationPermissionQueryHandler"/> class.
        /// </summary>


        public GetPageOperationPermissionQueryHandler(IMapper mapper, IUnitOfWork unitOfWork, ILogger<GetPageOperationPermissionQueryHandler> logger)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }
        #endregion

        #region Handler
        /// <summary>
        /// Processes the supplied GetPageOperationPermissionQuery.
        /// </summary>
        /// <param name="request">The request to process.</param>
        /// <param name="cancellationToken">The token used to observe cancellation.</param>
        /// <returns>The response produced for the request.</returns>


        public async Task<ApiResponse<GetHasAccessOperationDTO>> Handle(GetPageOperationPermissionQuery request, CancellationToken cancellationToken)
        {

            var response = new ApiResponse<GetHasAccessOperationDTO>();

            try
            {
                var requestDTO = request.CheckOperationPermissionRequest;


                // Repository se permission check karo
                bool result = await _unitOfWork.StoreProcedureRepository.GetHasAccessOperation(requestDTO);
                if (result)
                {
                    response.IsSucceeded = result;

                    response.Data = new GetHasAccessOperationDTO
                    {
                        Status = result, // Assign the bool directly
                        Message = "✅ Permission checked successfully.",
                        Success = result

                    };
                }
                else
                {
                    response.IsSucceeded = result;
                    response.Data = new GetHasAccessOperationDTO
                    {
                        Status = result, // Assign the bool directly
                        Message = "✅ Not have permission.",
                        Success = result

                    };
                }

              
            }
            catch (Exception ex)
            {
                response.IsSucceeded = false;
                response.Message = "❌ An error occurred while checking permission.";
                _logger.LogError($"🚨 Error in GetPageOperationPermissionQueryHandler: {ex.Message}");
            }

            return response;
        }
    
        #endregion
}
}
