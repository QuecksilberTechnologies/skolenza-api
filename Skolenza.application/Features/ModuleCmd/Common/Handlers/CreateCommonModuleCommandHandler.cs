// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles the request to Create Common Module.
// ================================================================

using skolenza.application.DTOs.Leave;
using skolenza.application.DTOs.Module.NewFolder;
using skolenza.application.DTOS.Module.CommonModule;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using skolenza.application.Features.ModuleCmd.Common.Commands;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.IRepositories;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace skolenza.application.Features.ModuleCmd.Common.Commands
{
    #region Command

    /// <summary>
    /// Represents the request to Create Common Module.
    /// </summary>
public class CreateCommonModuleCommand : IRequest<ApiResponse<List<GetCommonModuleResponseDTO>>>
    {

        public CreateCommonModuleRequestDTO DTO { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateCommonModuleCommand"/> class.
        /// </summary>

        public CreateCommonModuleCommand(CreateCommonModuleRequestDTO dTO)
        {
            DTO = dTO;
        }

    }

    #endregion
}

namespace skolenza.application.Features.ModuleCmd.Common.Handlers
{
/// <summary>
    /// Handles the creation of a new Common Module.
    /// </summary>
    public class CreateCommonModuleCommandHandler : IRequestHandler<CreateCommonModuleCommand, ApiResponse<List<GetCommonModuleResponseDTO>>>
    {
        #region Fields

        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CreateCommonModuleCommandHandler> _logger;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateCommonModuleCommandHandler"/> class.
        /// </summary>


        public CreateCommonModuleCommandHandler(
            IMapper mapper,
            IUnitOfWork unitOfWork,
            ILogger<CreateCommonModuleCommandHandler> logger)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }
        #endregion

        #region Handler
        /// <summary>
        /// Processes the supplied CreateCommonModuleCommand.
        /// </summary>
        /// <param name="request">The request to process.</param>
        /// <param name="cancellationToken">The token used to observe cancellation.</param>
        /// <returns>The response produced for the request.</returns>


        public async Task<ApiResponse<List<GetCommonModuleResponseDTO>>> Handle(CreateCommonModuleCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // ✅ Step 1: Validate input request
                if (request == null || request.DTO == null)
                {
                    _logger.LogWarning("❌ Invalid request received in CreatecommonModuleCommandHandler.");
                    return new ApiResponse<List<GetCommonModuleResponseDTO>>
                    {
                        IsSucceeded = false,
                        Message = "Invalid request data.",
                        Data = null
                    };
                }

                // ✅ Step 2: Call Repository to Add Common Module
                List<GetCommonModuleResponseDTO>? commonDto = await _unitOfWork.ModuleRepository.AddCommonModuleAsync(request.DTO);

                // ✅ Step 3: Null or empty validation
                if (commonDto == null || commonDto.Count == 0)
                {
                    _logger.LogWarning("⚠️ No Common module data returned after creation.");
                    return new ApiResponse<List<GetCommonModuleResponseDTO>>
                    {
                        IsSucceeded = false,
                        Message = "Module created, but no data returned.",
                        Data = null
                    };
                }

                // ✅ Step 4: Commit transaction
                await _unitOfWork.CommitTransactionAsync();

                // ✅ Step 5: Log success
                _logger.LogInformation("✅ Common Module created successfully. Total Modules fetched: {Count}", commonDto.Count);

                // ✅ Step 6: Return success response
                return new ApiResponse<List<GetCommonModuleResponseDTO>>
                {
                    IsSucceeded = true,
                    Message = "Module created successfully.",
                    Data = commonDto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error occurred while creating Common module.");
                return new ApiResponse<List<GetCommonModuleResponseDTO>>
                {
                    IsSucceeded = false,
                    Message = "Failed to create module.",
                    Data = null
                };
            }
        }


    
        #endregion
}
}
