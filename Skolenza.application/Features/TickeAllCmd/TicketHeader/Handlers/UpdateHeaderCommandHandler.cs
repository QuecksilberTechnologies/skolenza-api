// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles Update Header Command Handler requests.
// ================================================================

using AutoMapper;
using skolenza.application.DTOS.TicketDTO.Header;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.TickeAllCmd.TicketHeader.Handlers
{

    #region Command

    /// <summary>
    /// Represents the UpdateHeaderCommand application component.
    /// </summary>
    public class UpdateHeaderCommand : IRequest<ApiResponse<GetHeaderResponseDTO>>
    {
        public UpdateHeaderRequestDTO DTO { get; set; }

        public UpdateHeaderCommand(UpdateHeaderRequestDTO dto)
        {
            DTO = dto;
        }
    }

    /// <summary>
    /// Handles UpdateHeaderCommand requests.
    /// </summary>
        #endregion

    #region Handler

public class UpdateHeaderCommandHandler
        : IRequestHandler<UpdateHeaderCommand, ApiResponse<GetHeaderResponseDTO>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<UpdateHeaderCommandHandler> _logger;
        private readonly ICommonRequestService _commonRequestService;

        public UpdateHeaderCommandHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<UpdateHeaderCommandHandler> logger,
            ICommonRequestService commonRequestService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _commonRequestService = commonRequestService;
        }

        public async Task<ApiResponse<GetHeaderResponseDTO>> Handle(
            UpdateHeaderCommand request,
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

                // ===============================
                // 2️⃣ RBAC
                // ===============================
                //await _commonRequestService.HasAccessAsync(
                //    ModuleEnum.Ticket,
                //    OperationEnum.Update);

                // ===============================
                // 3️⃣ NULL SAFETY
                // ===============================
                if (request?.DTO == null || request.DTO.Id <= 0)
                    throw new ValidationErrorException("Invalid request data.");

                var dto = request.DTO;

                if (string.IsNullOrWhiteSpace(dto.HeaderName))
                    throw new ValidationErrorException("Header name is required.");

                // ===============================
                // 4️⃣ TRANSACTION
                // ===============================
                await _unitOfWork.BeginTransactionAsync();

                var entity = await _unitOfWork.TicketHeaderRepository
                    .GetByIdForInstitutionAsync(dto.Id, validation.InstitutionId);
                if (entity == null)
                    throw new ApiException("Header not found or could not be updated.", 404);

                _mapper.Map(dto, entity);
                entity.UpdatedById = validation.LoggedInStaffMemberId;
                entity.UpdatedDateTime = DateTime.UtcNow;

                var result = await _unitOfWork.TicketHeaderRepository.UpdateAsync(entity);

                if (result == null)
                    throw new ApiException("Header not found or could not be updated.", 404);

                await _unitOfWork.CommitTransactionAsync();

                // ===============================
                // 5️⃣ RESPONSE
                // ===============================
                var response = _mapper.Map<GetHeaderResponseDTO>(result);
                return ApiResponse<GetHeaderResponseDTO>
                    .Success(response, "Ticket header updated successfully.");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error updating TicketHeader");
                throw;
            }
        }
    }
    #endregion
}
