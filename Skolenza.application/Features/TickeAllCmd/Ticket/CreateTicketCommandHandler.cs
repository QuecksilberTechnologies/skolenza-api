// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles Create Ticket Command Handler requests.
// ================================================================

using skolenza.application.Common.Enums;
using skolenza.application.DTOS.Common;
using skolenza.application.DTOS.TicketDTO.Ticket;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;
using Microsoft.Extensions.Logging;


namespace skolenza.application.Features.TickeAllCmd.Ticket;

#region Command


/// <summary>
/// Represents the CreateTicketCommand application component.
/// </summary>
public class CreateTicketCommand : IRequest<ApiResponse<GetTicketResponseDTO>>
{
    public CreateTicketRequestDTO DTO { get; set; }

    public CreateTicketCommand(CreateTicketRequestDTO request)
    {
        DTO = request;
    }
}

/// <summary>
/// Handles CreateTicketCommand requests.
/// </summary>
#endregion

#region Handler

public class CreateTicketCommandHandler
: IRequestHandler<CreateTicketCommand, ApiResponse<GetTicketResponseDTO>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateTicketCommandHandler> _logger;
    private readonly ICommonRequestService _commonRequestService;

    public CreateTicketCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<CreateTicketCommandHandler> logger,
        ICommonRequestService commonRequestService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _commonRequestService = commonRequestService;
    }

    public async Task<ApiResponse<GetTicketResponseDTO>> Handle(
        CreateTicketCommand request,
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

            var dto = request.DTO ?? throw new ValidationErrorException("Invalid request");

            if (string.IsNullOrWhiteSpace(dto.Description))
                throw new ValidationErrorException("Description is required");

            // ===============================
            // 2️⃣ GET TICKET TYPE
            // ===============================
            var ticketType = await _unitOfWork.TicketTypeRepository
                .GetByIdAsync(dto.TicketTypeId, true);

            if (ticketType == null)
                throw new ValidationErrorException("Invalid TicketType");

            // ===============================
            // 3️⃣ ASSIGNMENT LOGIC
            // ===============================
            int status;
            int? assignedRoleId;
            bool isApproved;

            if (ticketType.IsApprovalRequired)
            {
                status = (int)TicketStatus.PendingApproval;
                assignedRoleId = ticketType.ApprovalId; //  FIXED
                isApproved = false;
            }
            else
            {
                status = (int)TicketStatus.Open;
                assignedRoleId = ticketType.ResponsibleRoleId;
                isApproved = true;
            }

            // ===============================
            // 4️⃣ CREATE TICKET
            // ===============================
            var ticket = new domain.Entity.Ticket
            {
                InstitutionId = validation.InstitutionId,
                TicketNumber = $"TKT-{DateTime.UtcNow:yyyyMMddHHmmssfff}",

                TicketClassificationId = dto.TicketClassificationId,
                TicketHeaderId = dto.TicketHeaderId,
                TicketTypeId = dto.TicketTypeId,

                Description = dto.Description,
                Priority = dto.Priority,
                Status = status,

                AssignedToRoleId = assignedRoleId,

                RequestedByUserId = validation.UserStaffMemberId,
                RequestedForUserId = dto.RequestedForUserId,

                IsApproved = isApproved,

                SLAHoursSnapshot = ticketType.SLAHours,
                SLAStartTime = DateTime.UtcNow,

                AddedById = validation.UserStaffMemberId,
                AddedDateTime = DateTime.UtcNow,

                IsActive = true,
                IsSoftDeleted = false
            };

            await _unitOfWork.TicketGenrationRepository.AddAsync(ticket);
            await _unitOfWork.SaveChangesAsync();

            // ===============================
            // 5️⃣ THREAD
            // ===============================
            var thread = new TicketThread
            {
                EntityType = (int)ThreadEntityType.Ticket,
                EntityId = ticket.Id,
                InstitutionId = validation.InstitutionId,
                AddedById = validation.UserStaffMemberId,
                AddedDateTime = DateTime.UtcNow,
                IsActive = true,
                IsSoftDeleted = false
            };

            await _unitOfWork.TicketThreadRepository.AddAsync(thread);
            await _unitOfWork.SaveChangesAsync();

            // ===============================
            // 6️⃣ MESSAGE
            // ===============================
            await _unitOfWork.ThreadMessageRepository.AddEntityAsync(new ThreadMessage
            {
                ThreadId = thread.Id,
                Message = "Ticket created",
                MessageType = (int)MessageType.System,
                AddedById = validation.UserStaffMemberId,
                AddedDateTime = DateTime.UtcNow,
                IsActive = true,
                IsSoftDeleted = false
            });


            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
            // ===============================
            // 11️⃣ RESPONSE
            // ===============================
            var result = await _unitOfWork.TicketGenrationRepository
                .GetByIdAsync(ticket.Id);
            //var resultDto = new GetTicketResponseDTO
            //{
            //    Id = result.Id,
            //    TicketNumber = result.TicketNumber,
            //    TicketClassificationId = result.TicketClassificationId,
            //    TicketHeaderId = result.TicketHeaderId,
            //    TicketTypeId = result.TicketTypeId,
            //    Description = result.Description,
            //    Priority = result.Priority,
            //    Status = result.Status,
            //    AssignedToRoleId = result.AssignedToRoleId,
            //    AssignedToUserId = result.AssignedToUserId,
            //    RequestedForUserId = result.RequestedForUserId,
            //    RequestedByUserId = result.RequestedByUserId,
            //    IsApproved = result.IsApproved,
            //    ApprovedByUserId = result.ApprovedByUserId,
            //    ApprovedDateTime = result.ApprovedDateTime,
            //    SLAHours = result.SLAHours,
            //    SLAStartTime = result.SLAStartTime,
            //    SLAEndTime = result.SLAEndTime,
            //    IsSLABreached = result.IsSLABreached,
            //    AddedDateTime = result.AddedDateTime
            //};
            return ApiResponse<GetTicketResponseDTO>
                .Success(result, "Ticket created successfully");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Error creating ticket");
            throw;
        }
    }
}

#endregion
