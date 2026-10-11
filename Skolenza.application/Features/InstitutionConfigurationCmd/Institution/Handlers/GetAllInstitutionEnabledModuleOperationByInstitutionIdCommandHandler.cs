// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Processes the GetInstitutionEnabledModuleCommand use case.
// ================================================================

using skolenza.application.DTOs.Institution;
using skolenza.application.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AutoMapper;
using skolenza.application.Features.InstitutionConfigurationCmd.Institution.Commands;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IRepositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.InstitutionConfigurationCmd.Institution.Commands
{
    #region Command

    /// <summary>
    /// Represents the command request for Get Institution Enabled Module.
    /// </summary>
public class GetInstitutionEnabledModuleCommand : IRequest<ApiResponse<GetModuleHierarchyResponseDTO>>
    {

        public InstitutionEnabledModuleRequestDTO dto { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="GetInstitutionEnabledModuleCommand"/> class.
        /// </summary>

        public GetInstitutionEnabledModuleCommand(InstitutionEnabledModuleRequestDTO dto)
        {
            this.dto = dto;
        }

    }

    #endregion
}

namespace skolenza.application.Features.InstitutionConfigurationCmd.Institution.Handlers
{
    /// <summary>
    /// Handles the request for Get Institution Enabled Module.
    /// </summary>
public class GetAllInstitutionEnabledModuleOperationByInstitutionIdCommandHandler : IRequestHandler<GetInstitutionEnabledModuleCommand, ApiResponse<GetModuleHierarchyResponseDTO>>
    {
        #region Fields

        private readonly IInstitutionModuleConfigurationRepository _institutionModuleConfigurationRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetAllInstitutionOperationsCommandHandler> _logger;
        private readonly ICommonRequestService _commonRequestService;
        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="GetAllInstitutionEnabledModuleOperationByInstitutionIdCommandHandler"/> class.
        /// </summary>

        public GetAllInstitutionEnabledModuleOperationByInstitutionIdCommandHandler(
            IInstitutionModuleConfigurationRepository institutionModuleConfigurationRepository,
            IMapper mapper,
            IUnitOfWork unitOfWork,
            ILogger<GetAllInstitutionOperationsCommandHandler> logger,
            ICommonRequestService commonRequestService)
        {
            _institutionModuleConfigurationRepository = institutionModuleConfigurationRepository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _commonRequestService = commonRequestService;
        }
        #endregion

        #region Handler

        /// <summary>
        /// Handles the request asynchronously.
        /// </summary>
        /// <param name="request">The request to process.</param>
        /// <param name="cancellationToken">A token used to cancel the operation.</param>
        /// <returns>The response produced by handling the request.</returns>

        public async Task<ApiResponse<GetModuleHierarchyResponseDTO>> Handle(GetInstitutionEnabledModuleCommand request, CancellationToken cancellationToken)
           {
            try
              {
                //  Request null check
                if (request == null)
                {
                    _logger.LogWarning("GetInstitutionEnabledModuleOperationCommand is null.");
                    return new ApiResponse<GetModuleHierarchyResponseDTO>
                    {
                        IsSucceeded = false,
                        Message = "Request cannot be null.",
                        Data = null
                    };
                }

                var validation = await _commonRequestService.ValidateInstitutionUserRequestAsync();
                if (!validation.Success)
                {
                    throw new UnauthorizedAccessException(validation.ErrorMessage);
                }

                var responseDTO = await _institutionModuleConfigurationRepository
                    .GetAllInstitutionEnabledModulesAsync(new skolenza.domain.Entity.InstitutionEnabledOperation
                    {
                        InstitutionId = validation.InstitutionId
                    });


                return new ApiResponse<GetModuleHierarchyResponseDTO>
                {
                    IsSucceeded = true,
                    Message = "Modules and operations fetched successfully.",
                    Data = responseDTO
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching module operations for institution.");
                return new ApiResponse<GetModuleHierarchyResponseDTO>
                {
                    IsSucceeded = false,
                    Message = "Something went wrong while fetching module operations.",
                    Data = null
                };
            }
        }

        #endregion
}
}
