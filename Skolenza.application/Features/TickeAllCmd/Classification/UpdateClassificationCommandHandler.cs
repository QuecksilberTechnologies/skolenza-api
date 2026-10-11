// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles Update Classification Command Handler requests.
// ================================================================

using AutoMapper;
using skolenza.application.Common.Helpers;
using skolenza.application.Constants;
using skolenza.application.DTOS.TicketDTO.Classification;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.TickeAllCmd.Classification
{

    #region Command

    /// <summary>
    /// Represents the UpdateClassificationCommand application component.
    /// </summary>
    public class UpdateClassificationCommand : IRequest<ApiResponse<GetClassificationResponseDTO>>
    {
        public UpdateClassificationRequestDTO DTO { get; set; }

        public UpdateClassificationCommand(UpdateClassificationRequestDTO dto)
        {
            DTO = dto;
        }
    }

    /// <summary>
    /// Handles UpdateClassificationCommand requests.
    /// </summary>
        #endregion

    #region Handler

public class UpdateClassificationCommandHandler
        : IRequestHandler<UpdateClassificationCommand, ApiResponse<GetClassificationResponseDTO>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<UpdateClassificationCommandHandler> _logger;
        private readonly ICommonRequestService _commonRequestService;

        public UpdateClassificationCommandHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<UpdateClassificationCommandHandler> logger,
            ICommonRequestService commonRequestService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _commonRequestService = commonRequestService;
        }

        public async Task<ApiResponse<GetClassificationResponseDTO>> Handle(
            UpdateClassificationCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                // ===============================
                // 1️⃣ VALIDATION
                // ===============================
                var validation = await _commonRequestService.ValidateInstitutionUserRequestAsync();

                if (!validation.Success)
                    throw new UnauthorizedAccessException(validation.ErrorMessage);

                long institutionId = validation.InstitutionId;
                long userStaffMemberId = validation.LoggedInStaffMemberId;
                int tokenRoleId = validation.RoleId;
                if (institutionId <= 0 || userStaffMemberId <= 0 || tokenRoleId <= 0)
                    throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);

                var permissionResult = await _unitOfWork.StoreProcedureRepository.CheckInstitutionStaffMemberPermissionAsync(institutionId, userStaffMemberId, tokenRoleId, request.DTO.ModuleId, request.DTO.OperationId, cancellationToken);
                InstitutionRuntimePermissionValidator.EnsureAllowed(permissionResult);

                // ===============================
                // 3️⃣ NULL SAFETY
                // ===============================
                if (request?.DTO == null || request.DTO.Id <= 0)
                    throw new ValidationErrorException("Invalid request data.");

                var dto = request.DTO;

                if (string.IsNullOrWhiteSpace(dto.ClassificationName))
                    throw new ValidationErrorException("ClassificationName is required.");

                // ===============================
                // 4️⃣ TRANSACTION
                // ===============================
                await _unitOfWork.BeginTransactionAsync();

                var entity = await _unitOfWork.TicketClassificationRepository
                    .GetByIdForInstitutionAsync(dto.Id, institutionId);
                if (entity == null)
                    throw new ApiException("Classification not found or could not be updated.", 404);

                _mapper.Map(dto, entity);
                entity.UpdatedById = userStaffMemberId;
                entity.UpdatedDateTime = DateTime.UtcNow;

                var result = await _unitOfWork.TicketClassificationRepository.UpdateAsync(entity);

                if (result == null)
                    throw new ApiException("Classification not found or could not be updated.", 404);

                await _unitOfWork.CommitTransactionAsync();

                // ===============================
                // 5️⃣ RESPONSE
                // ===============================
                var response = _mapper.Map<GetClassificationResponseDTO>(result);
                return ApiResponse<GetClassificationResponseDTO>
                    .Success(response, "Ticket classification updated successfully.");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error updating TicketClassification");
                throw;
            }
        }
    }
    #endregion
}
