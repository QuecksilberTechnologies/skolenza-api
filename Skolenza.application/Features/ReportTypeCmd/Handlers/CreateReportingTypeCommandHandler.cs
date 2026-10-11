// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles Create Reporting Type Command Handler requests.
// ================================================================

using AutoMapper;
using skolenza.application.Common.Helpers;
using skolenza.application.Constants;
using skolenza.application.DTOs.Manager.ReportingType;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.ReportTypeCmd.Handlers
{

    #region Command

    //  Command
    /// <summary>
    /// Represents the CreateReportingTypeCommand application component.
    /// </summary>
    public class CreateReportingTypeCommand : IRequest<ApiResponse<GetReportingTypeResponseDTO>>
    {
        public CreateReportingTypeRequestDTO DTO { get; set; }

        public CreateReportingTypeCommand(CreateReportingTypeRequestDTO dto)
        {
            DTO = dto;
        }
    }

    //  Handler
    /// <summary>
    /// Handles CreateReportingTypeCommand requests.
    /// </summary>
        #endregion

    #region Handler

public class CreateReportingTypeCommandHandler
        : IRequestHandler<CreateReportingTypeCommand, ApiResponse<GetReportingTypeResponseDTO>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<CreateReportingTypeCommandHandler> _logger;
        private readonly ICommonRequestService _commonRequestService;

        public CreateReportingTypeCommandHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<CreateReportingTypeCommandHandler> logger,
            ICommonRequestService commonRequestService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _commonRequestService = commonRequestService;
        }

        public async Task<ApiResponse<GetReportingTypeResponseDTO>> Handle(
            CreateReportingTypeCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("🔹 CreateReportingType started");

                #region Institution Request Validation

                var validation = await _commonRequestService.ValidateInstitutionUserRequestAsync();
                if (!validation.Success)
                {
                    throw new UnauthorizedAccessException(
                        validation.ErrorMessage ?? AppConstants.ErrorMessages.Unauthorized);
                }

                #endregion

                #region Trusted Request Context

                long userStaffMemberId = validation.LoggedInStaffMemberId;
                long institutionId = validation.InstitutionId;
                int tokenRoleId = validation.RoleId;

                if (userStaffMemberId <= 0 || institutionId <= 0 || tokenRoleId <= 0)
                {
                    _logger.LogWarning(
                        "Invalid Institution authorization context while creating Reporting Type. InstitutionId: {InstitutionId}, StaffMemberId: {StaffMemberId}, TokenRoleId: {TokenRoleId}",
                        institutionId, userStaffMemberId, tokenRoleId);
                    throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
                }

                #endregion

                #region Runtime Permission Validation

                var permissionResult = await _unitOfWork.StoreProcedureRepository
                    .CheckInstitutionStaffMemberPermissionAsync(
                        institutionId,
                        userStaffMemberId,
                        tokenRoleId,
                        request.DTO.ModuleId,
                        request.DTO.OperationId,
                        cancellationToken);

                InstitutionRuntimePermissionValidator.EnsureAllowed(permissionResult);

                #endregion

                // ===============================
                // 2️⃣ NULL CHECK
                // ===============================
                if (request?.DTO == null)
                    throw new ValidationErrorException("Invalid request data.");

                // ===============================
                // 3️⃣ REPOSITORY CALL
                // ===============================
                var entity = _mapper.Map<skolenza.domain.Entity.ReportingType>(request.DTO);
                entity.InstitutionId = institutionId;
                entity.AddedById = userStaffMemberId;
                entity.AddedDateTime = DateTime.UtcNow;
                entity.IsSoftDeleted = false;

                var created = await _unitOfWork.ReportingTypeRepository.AddAsync(entity);

                if (created == null)
                    throw new Exception("ReportingType creation failed.");

                // ===============================
                // 4️⃣ SUCCESS
                // ===============================
                return ApiResponse<GetReportingTypeResponseDTO>
                    .Success(_mapper.Map<GetReportingTypeResponseDTO>(created), "ReportingType created successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateReportingType");
                throw; //  middleware handle karega
            }
        }
    }
    #endregion
}
