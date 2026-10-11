using AutoMapper;
using skolenza.application.DTOS.TicketDTO.Classification;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IPermission;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Reflection;
namespace skolenza.application.Features.TickeAllCmd.Classification
{
    public class DeleteClassificationCommand : IRequest<ApiResponse<bool>>
    {
        public DeleteClassificationRequestDTO DTO { get; set; }

        public DeleteClassificationCommand(DeleteClassificationRequestDTO dto)
        {
            DTO = dto;
        }

    }
    public class DeleteClassificationCommandHandler
        : IRequestHandler<DeleteClassificationCommand, ApiResponse<bool>>
    {
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeleteClassificationCommandHandler> _logger;
        private readonly ICommonRequestService _commonRequestService;

        public DeleteClassificationCommandHandler(
            IMapper mapper,
            IUnitOfWork unitOfWork,
            ILogger<DeleteClassificationCommandHandler> logger,
            ICommonRequestService commonRequestService)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _commonRequestService = commonRequestService;
        }
        public async Task<ApiResponse<bool>> Handle(
    DeleteClassificationCommand request,
    CancellationToken cancellationToken)
        {
            string methodName = nameof(DeleteClassificationCommandHandler);

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                _logger.LogInformation("🔹 {Method} started", methodName);

                // ===============================
                // 1️⃣ VALIDATION
                // ===============================
                var validation = await _commonRequestService.ValidateInstitutionUserRequestAsync();

                if (!validation.Success)
                    throw new UnauthorizedAccessException(validation.ErrorMessage);

                // ===============================
                // 2️⃣ NULL SAFETY
                // ===============================
                if (request?.DTO == null || request.DTO.Id <= 0)
                    throw new ValidationErrorException(
                        "Invalid request.",
                        new List<string> { "Valid Id is required." });

                // ===============================
                // 3️⃣ SET COMMON PROPS
                // ===============================
 

                // ===============================
                // 4️⃣ RBAC PERMISSION CHECK 🔥
                // ===============================
                //var hasPermission = await _permissionService.HasAccessAsync(
                //    validation.RoleId,
                //    "TicketClassification",   // ModuleName (DB match)
                //    "Delete"                  // Operation
                //);

                //if (!hasPermission)
                //    throw new UnauthorizedAccessException("You do not have permission to delete classification.");

                // ===============================
                // 5️⃣ DELETE
                // ===============================
                _logger.LogInformation("🗑️ Deleting classification Id={Id}", request.DTO.Id);

                var isDeleted = await _unitOfWork.TicketClassificationRepository
                    .DeleteAsync(request.DTO, validation.LoggedInStaffMemberId);

                if (!isDeleted)
                    throw new KeyNotFoundException("Classification not found or already deleted.");

                // ===============================
                // 6️⃣ COMMIT
                // ===============================
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("✅ Classification deleted successfully | Id={Id}", request.DTO.Id);

                return ApiResponse<bool>.Success(true, "Classification deleted successfully.");
            }
            catch (Exception ex)
            {
                // ===============================
                // 🔁 ROLLBACK
                // ===============================
                await _unitOfWork.RollbackTransactionAsync();

                _logger.LogError(ex, "❌ {Method} failed | Id={Id}", methodName, request?.DTO?.Id);

                throw; // 🔥 middleware handle करेगा
            }
        }

    }
}
