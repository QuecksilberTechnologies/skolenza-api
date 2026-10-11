// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles Get All Institution Operations Command Handler requests.
// ================================================================

using AutoMapper;
using skolenza.application.DTOs.Institution;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.InstitutionConfigurationCmd.Institution.Handlers
{

    #region Command

    //  FIXED COMMAND
    /// <summary>
    /// Represents the GetAllInstitutionOperationsCommand application component.
    /// </summary>
    public class GetAllInstitutionOperationsCommand : IRequest<ApiResponse<InstitutionEnabledOperationsResponseDTO>>
    {
        public InstitutionEnabledOperationsRequestDTO Request { get; set; }

        public GetAllInstitutionOperationsCommand(InstitutionEnabledOperationsRequestDTO request)
        {
            Request = request;
        }
    }

    //  FIXED HANDLER
    /// <summary>
    /// Handles GetAllInstitutionOperationsCommand requests.
    /// </summary>
        #endregion

    #region Handler

public class GetAllInstitutionOperationsCommandHandler
        : IRequestHandler<GetAllInstitutionOperationsCommand, ApiResponse<InstitutionEnabledOperationsResponseDTO>>
    {
        private readonly IInstitutionModuleConfigurationRepository _institutionModuleConfigurationRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetAllInstitutionOperationsCommandHandler> _logger;
        private readonly ICommonRequestService _commonRequestService;

        public GetAllInstitutionOperationsCommandHandler(
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

        public async Task<ApiResponse<InstitutionEnabledOperationsResponseDTO>> Handle(
            GetAllInstitutionOperationsCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("🔹 GetAllInstitutionOperations started");

                // ===============================
                // 1️⃣ VALIDATION
                // ===============================
                if (request?.Request == null)
                    throw new ValidationErrorException("Invalid request data.");

                var validation = await _commonRequestService
                    .ValidateInstitutionUserRequestAsync();

                if (!validation.Success)
                    throw new UnauthorizedAccessException(validation.ErrorMessage);

                // ===============================
                // 2️⃣ PERMISSION CHECK (RBAC)
                // ===============================
                //var hasAccess = await _permissionService.HasAccessAsync(
                //    validation.RoleId,
                //    Modules.Role,
                //    Operations.Add);

                //if (!hasAccess)
                //    throw new UnauthorizedAccessException("Access denied.");


                // ===============================
                // 2️⃣ SET CONTEXT
                // ===============================
                InstitutionEnabledOperation institutionEnabledOperation = new InstitutionEnabledOperation
                {
                    InstitutionId = validation.InstitutionId,

                };
                // ===============================
                // 3️⃣ FETCH DATA
                // ===============================
                var responseDTO = await _unitOfWork
                    .UserRolesPermissionOnModuleRepository.GetAllInstitutionModuleWithOperation(institutionEnabledOperation);

                // ===============================
                // 4️⃣ RESPONSE
                // ===============================
                return new ApiResponse<InstitutionEnabledOperationsResponseDTO>
                {
                    IsSucceeded = true,
                    Message = "Modules and operations fetched successfully.",
                    Data = responseDTO
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching module operations for institution.");

                return new ApiResponse<InstitutionEnabledOperationsResponseDTO>
                {
                    IsSucceeded = false,
                    Message = "Something went wrong while fetching module operations.",
                    Data = null
                };
            }
        }
    }
    #endregion
}

