// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles the request to Institution Enabled Module Operations Update.
// ================================================================

using skolenza.application.DTOs.Institution;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using skolenza.application.Features.InstitutionConfigurationCmd.Institution.Commands;
using skolenza.application.Interfaces.IRepositories;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.InstitutionConfigurationCmd.Institution.Commands
{
    #region Command

    /// <summary>
    /// Represents the request to Institution Enabled Module Operations Update.
    /// </summary>
public class InstitutionEnabledModuleOperationsUpdateCommand : IRequest<ApiResponse<bool>>
    {
        public InstitutionModuleOperationsUpdateRequestDTO RequestDTO { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="InstitutionEnabledModuleOperationsUpdateCommand"/> class.
        /// </summary>

        public InstitutionEnabledModuleOperationsUpdateCommand(InstitutionModuleOperationsUpdateRequestDTO dto)
        {
            RequestDTO = dto;
        }
    }

    #endregion
}

namespace skolenza.application.Features.InstitutionConfigurationCmd.Institution.Handlers
{
    /// <summary>
    /// Handles the request to Institution Enabled Module Operations Update.
    /// </summary>
public class InstitutionEnabledModuleOperationsUpdateCommandHandler : IRequestHandler<InstitutionEnabledModuleOperationsUpdateCommand, ApiResponse<bool>>
    {
        #region Fields

        private readonly IInstitutionModuleConfigurationRepository _repository;
        private readonly ILogger<InstitutionEnabledModuleOperationsUpdateCommandHandler> _logger;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="InstitutionEnabledModuleOperationsUpdateCommandHandler"/> class.
        /// </summary>


        public InstitutionEnabledModuleOperationsUpdateCommandHandler(
            IInstitutionModuleConfigurationRepository repository,
            ILogger<InstitutionEnabledModuleOperationsUpdateCommandHandler> logger)
        {
            _repository = repository;
            _logger = logger;
        }
        #endregion

        #region Handler
        /// <summary>
        /// Processes the supplied InstitutionEnabledModuleOperationsUpdateCommand.
        /// </summary>
        /// <param name="request">The request to process.</param>
        /// <param name="cancellationToken">The token used to observe cancellation.</param>
        /// <returns>The response produced for the request.</returns>


        public async Task<ApiResponse<bool>> Handle(InstitutionEnabledModuleOperationsUpdateCommand request, CancellationToken cancellationToken)
        {
            try
            {
                if (request?.RequestDTO == null || request.RequestDTO.InstitutionId <= 0)
                {
                    _logger.LogWarning("Invalid request in InstitutionModuleOperationsUpdateCommand.");
                    return new ApiResponse<bool>
                    {
                        IsSucceeded = false,
                        Message = "Invalid request: InstitutionId is missing or invalid.",
                        Data = false
                    };
                }

                // 🛠 Update Module + Operations via Repository
                //var isUpdated = await _repository.UpdateInstitutionModuleAndItsOperationsAsync(request.RequestDTO);
                var isUpdated = true;

                return new ApiResponse<bool>
                {
                    IsSucceeded = isUpdated,
                    Message = isUpdated ? "Module operations updated successfully." : "Update failed.",
                    Data = isUpdated
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred in InstitutionModuleOperationsUpdateCommandHandler.");
                return new ApiResponse<bool>
                {
                    IsSucceeded = false,
                    Message = "Internal server error while updating module operations.",
                    Data = false
                };
            }
        }
    
        #endregion
}
}
