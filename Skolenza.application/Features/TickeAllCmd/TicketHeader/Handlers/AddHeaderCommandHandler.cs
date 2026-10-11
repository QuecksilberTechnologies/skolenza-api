// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles Add Header Command Handler requests.
// ================================================================

using AutoMapper;
using skolenza.application.DTOS.Common;
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
    /// Represents the AddHeaderCommand application component.
    /// </summary>
    public class AddHeaderCommand : IRequest<ApiResponse<GetHeaderResponseDTO>>
    {
        public AddHeaderRequestDTO DTO { get; set; }

        public AddHeaderCommand(AddHeaderRequestDTO dto)
        {
            DTO = dto;
        }
    }

    /// <summary>
    /// Handles AddHeaderCommand requests.
    /// </summary>
        #endregion

    #region Handler

public class AddHeaderCommandHandler
        : IRequestHandler<AddHeaderCommand, ApiResponse<GetHeaderResponseDTO>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<AddHeaderCommandHandler> _logger;
        private readonly ICommonRequestService _commonRequestService;

        public AddHeaderCommandHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<AddHeaderCommandHandler> logger,
            ICommonRequestService commonRequestService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _commonRequestService = commonRequestService;
        }

        public async Task<ApiResponse<GetHeaderResponseDTO>> Handle(
            AddHeaderCommand request,
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
                //    OperationEnum.Create);

                // ===============================
                // 3️⃣ NULL SAFETY
                // ===============================
                if (request?.DTO == null)
                    throw new ValidationErrorException("Invalid request data.");

                var dto = request.DTO;

                if (string.IsNullOrWhiteSpace(dto.HeaderName))
                    throw new ValidationErrorException("Header name is required.");

                // ===============================
                // 4️⃣ TRANSACTION
                // ===============================
                await _unitOfWork.BeginTransactionAsync();

                var entity = _mapper.Map<skolenza.domain.Entity.TicketHeader>(dto);
                entity.InstitutionId = validation.InstitutionId;
                entity.AddedById = validation.LoggedInStaffMemberId;
                entity.AddedDateTime = DateTime.UtcNow;
                entity.IsSoftDeleted = false;

                var result = await _unitOfWork.TicketHeaderRepository.AddAsync(entity);

                if (result == null)
                    throw new ApiException("Failed to add header.", 500);

                await _unitOfWork.CommitTransactionAsync();

                // ===============================
                // 5️⃣ RESPONSE
                // ===============================
                var response = _mapper.Map<GetHeaderResponseDTO>(result);
                return ApiResponse<GetHeaderResponseDTO>
                    .Success(response, "Header added successfully.");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error in AddHeaderCommandHandler");
                throw;
            }
        }
    }
    #endregion
}
