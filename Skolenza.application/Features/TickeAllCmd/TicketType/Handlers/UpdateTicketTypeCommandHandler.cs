// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles Update Ticket Type Command Handler requests.
// ================================================================

using AutoMapper;
using skolenza.application.DTOS.Common;
using skolenza.application.DTOS.Role;
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

    #region Command

    /// <summary>
    /// Represents the UpdateTicketTypeCommand application component.
    /// </summary>
    public class UpdateTicketTypeCommand : IRequest<ApiResponse<bool>>
    {
        public UpdateTicketTypeRequestDTO DTO { get; }

        public UpdateTicketTypeCommand(UpdateTicketTypeRequestDTO dto)
        {
            DTO = dto;
        }
    }
    /// <summary>
    /// Handles UpdateTicketTypeCommand requests.
    /// </summary>
        #endregion

    #region Handler

public class UpdateTicketTypeCommandHandler
     : IRequestHandler<UpdateTicketTypeCommand, ApiResponse<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<UpdateTicketTypeCommandHandler> _logger;
        private readonly ICommonRequestService _commonRequestService;

        public UpdateTicketTypeCommandHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<UpdateTicketTypeCommandHandler> logger,
            ICommonRequestService commonRequestService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _commonRequestService = commonRequestService;
        }

        public async Task<ApiResponse<bool>> Handle(
            UpdateTicketTypeCommand request,
            CancellationToken cancellationToken)
        {
            await _unitOfWork.BeginTransactionAsync();

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
                if (request?.DTO == null)
                    throw new ValidationErrorException("Invalid request data.");

                var dto = request.DTO;

                // ===============================
                // 4️⃣ BUSINESS VALIDATION
                // ===============================
                if (string.IsNullOrWhiteSpace(dto.TicketTypeName))
                    throw new ValidationErrorException("TicketTypeName is required.");

                if (dto.IsApprovalRequired && dto.ApprovalRoleId == null)
                    throw new ValidationErrorException("ApprovalRoleId is required.");


                if (dto.SLAHours != null && dto.SLAHours <= 0)
                    throw new ValidationErrorException("SLAHours must be greater than 0.");
                // ===============================
                // 5️⃣ FK VALIDATION
                // ===============================

                // Header check
                var header = await _unitOfWork.TicketHeaderRepository
                    .GetByIdAsync(dto.TicketHeaderId);

                if (header == null)
                    throw new ValidationErrorException("Invalid TicketHeaderId.");


                //  Responsible Role check
                var responsibleRoleRequest = new GetSingleRoleRequestDTO
                {
                    Id = dto.ResponsibleRoleId
                };

                var responsibleRole = await _unitOfWork.RoleRepository
                    .GetByIdAsync1(responsibleRoleRequest);

                if (responsibleRole == null)
                    throw new ValidationErrorException("Invalid ResponsibleRoleId.");


                //  Approval Role check
                if (dto.IsApprovalRequired)
                {
                    var approvalRoleRequest = new GetSingleRoleRequestDTO
                    {
                        Id = dto.ApprovalRoleId ?? 0
                    };

                    var approvalRole = await _unitOfWork.RoleRepository
                        .GetByIdAsync1(approvalRoleRequest);

                    if (approvalRole == null)
                        throw new ValidationErrorException("Invalid ApprovalRoleId.");
                }

                //  Is Attachment requirement check
                if (dto.IsAttachmentRequired)
                {
                    var attachmentRequest  = new GetSingleRoleRequestDTO
                    {
                        Id = dto.IsAttachmentRequired ? 1 : 0
                    };

                    var attachmentRequire = await _unitOfWork.RoleRepository
                        .GetByIdAsync1(attachmentRequest);
                       if (attachmentRequire == null)
                        throw new ValidationErrorException("Is Attachment required.");
                }

                // ===============================
                // 6️⃣ UPDATE CALL
                // ===============================
                var entity = await _unitOfWork.TicketTypeRepository
                    .GetByIdForInstitutionAsync(dto.Id, validation.InstitutionId);
                if (entity == null)
                throw new ApiException("TicketType update failed.", 500);

                _mapper.Map(dto, entity);
                entity.UpdatedById = validation.LoggedInStaffMemberId;
                entity.UpdatedDateTime = DateTime.UtcNow;

                var updated = await _unitOfWork.TicketTypeRepository.UpdateAsync(entity);
                if (updated == null)
                    throw new ApiException("TicketType update failed.", 500);

                // ===============================
                // 7️⃣ COMMIT
                // ===============================
                await _unitOfWork.CommitTransactionAsync();

                return ApiResponse<bool>.Success(true, "TicketType updated successfully.");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error updating TicketType");
                throw;
            }
        }
    }
    #endregion
}
