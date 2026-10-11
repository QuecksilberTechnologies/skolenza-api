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

    public class DeleteHeaderCommand : IRequest<ApiResponse<bool>>
    {
        public DeleteHeaderRequestDTO DTO { get; set; }

        public DeleteHeaderCommand(DeleteHeaderRequestDTO dto)
        {
            DTO = dto;
        }

    }


    /// <summary>
    /// Handles the deletion of a Ticket Header (soft delete).
    /// </summary>
    public class DeleteHeaderCommandHandler
        : IRequestHandler<DeleteHeaderCommand, ApiResponse<bool>>
    {
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeleteHeaderCommandHandler> _logger;
        private readonly ICommonRequestService _commonRequestService;
        public DeleteHeaderCommandHandler(
            IMapper mapper,
            IUnitOfWork unitOfWork,
            ILogger<DeleteHeaderCommandHandler> logger,
            ICommonRequestService commonRequestService)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _commonRequestService = commonRequestService;
        }

        public async Task<ApiResponse<bool>> Handle(
     DeleteHeaderCommand request,
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
                //    OperationEnum.Delete);

                // ===============================
                // 3️⃣ NULL SAFETY
                // ===============================
                if (request?.DTO == null || request.DTO.Id <= 0)
                    throw new ValidationErrorException("Invalid request data.");
                
              

                // ===============================
                // 4️⃣ TRANSACTION
                // ===============================
                await _unitOfWork.BeginTransactionAsync();

                var result = await _unitOfWork.TicketHeaderRepository
                    .DeleteAsync(request.DTO, validation.LoggedInStaffMemberId);

                if (!result)
                    throw new ApiException("Header not found or could not be deleted.", 404);

                await _unitOfWork.CommitTransactionAsync();

                // ===============================
                // 5️⃣ RESPONSE
                // ===============================
                return ApiResponse<bool>
                    .Success(true, "Ticket header deleted successfully.");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error deleting TicketHeader");
                throw;
            }
        }
    }
}
