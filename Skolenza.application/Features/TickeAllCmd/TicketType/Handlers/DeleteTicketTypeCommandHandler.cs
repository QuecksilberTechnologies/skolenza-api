using AutoMapper;
using skolenza.application.DTOS.Common;
using skolenza.application.DTOS.TicketDTO.TicketType;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;
namespace skolenza.application.Features.TickeAllCmd.TicketType.Handlers
{
    public class DeleteTicketTypeCommand : IRequest<ApiResponse<bool>>
    {

        public DeleteTicketTypeRequestDTO? DTO { get; set; }

        public DeleteTicketTypeCommand(DeleteTicketTypeRequestDTO dTO)
        {
            DTO = dTO;
        }
    }

    public class DeleteTicketTypeCommandHandler
   : IRequestHandler<DeleteTicketTypeCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeleteTicketTypeCommandHandler> _logger;
        private readonly ICommonRequestService _commonRequestService;

        public DeleteTicketTypeCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<DeleteTicketTypeCommandHandler> logger,
            ICommonRequestService commonRequestService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _commonRequestService = commonRequestService;
        }

        public async Task<ApiResponse<bool>> Handle(
            DeleteTicketTypeCommand request,
            CancellationToken cancellationToken)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                // 1️⃣ COMMON VALIDATION
                var validation = await _commonRequestService.ValidateInstitutionUserRequestAsync();
                if (!validation.Success)
                    throw new UnauthorizedAccessException(validation.ErrorMessage);

                // 2️⃣ RBAC
                //await _commonRequestService.HasAccessAsync(
                //    ModuleEnum.Ticket,
                //    OperationEnum.Delete);

                // 3️⃣ NULL + ID VALIDATION
                if (request?.DTO == null || request.DTO.Id <= 0)
                    throw new ValidationErrorException("Invalid TicketType Id.");

               

                // 4️⃣ REPOSITORY CALL
                var isDeleted = await _unitOfWork.TicketTypeRepository
                    .DeleteAsync(request.DTO.Id, validation.UserStaffMemberId);

                if (!isDeleted)
                    throw new ApiException("TicketType deletion failed.", 500);

                // 5️⃣ COMMIT
                await _unitOfWork.CommitTransactionAsync();

                return ApiResponse<bool>.Success(true, "TicketType deleted successfully.");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error deleting TicketType");
                throw;
            }
        }
    }

}
