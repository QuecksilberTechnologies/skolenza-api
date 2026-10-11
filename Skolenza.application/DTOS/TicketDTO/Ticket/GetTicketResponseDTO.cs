using skolenza.application.DTOS.Pagination;
using System;
using System.Collections.Generic;
using System.Text;

namespace skolenza.application.DTOS.TicketDTO.Ticket
{
    public class GetTicketResponseDTO :BaseResponse
    {
        // 🔹 Identity
        public long Id { get; set; }
        public string TicketNumber { get; set; } = null!;

        // 🔹 Classification
        public int? TicketClassificationId { get; set; }
        public string? TicketClassificationName { get; set; }

        public long TicketHeaderId { get; set; }
        public string? TicketHeaderName { get; set; }

        public long TicketTypeId { get; set; }
        public string? TicketTypeName { get; set; }

        // 🔹 Core
        public string Description { get; set; } = null!;
        public int Priority { get; set; }

        // 🔹 Status (UI ke liye important 🔥)
        public int Status { get; set; }
        public string StatusName { get; set; } = null!;

        // 🔥 Assignment Info
        public int? AssignedToRoleId { get; set; }
        public string? AssignedToRoleName { get; set; }

        public long? AssignedToUserId { get; set; }
        public string? AssignedToUserName { get; set; }

        // 🔹 Request Info
        public long? RequestedForUserId { get; set; }
        public string? RequestedForUserName { get; set; }

        public long RequestedByUserId { get; set; }
        public string? RequestedByUserName { get; set; }

        // 🔥 Approval Info
        public bool? IsApproved { get; set; }
        public long? ApprovedByUserId { get; set; }
        public string? ApprovedByUserName { get; set; }
        public DateTime? ApprovedDateTime { get; set; }

        // 🔥 SLA Info
        public int? SLAHours { get; set; }
        public DateTime? SLAStartTime { get; set; }
        public DateTime? SLAEndTime { get; set; }
        public bool? IsSLABreached { get; set; }

        // 🔹 Timeline
        public DateTime AddedDateTime { get; set; }

        // 🔥 Helpful UI Flags (VERY IMPORTANT)
        public bool IsEditable { get; set; }
        public bool CanApprove { get; set; }
        public bool CanAssign { get; set; }
        public TicketAttachmentDTO? TicketAttachmentDTO { get; set; } = null;
    }
    public class TicketAttachmentDTO
    {
        public long Id { get; set; }

        public string FileName { get; set; } = null!;

        public string FilePath { get; set; } = null!;

        public string? FileType { get; set; }

        public long? FileSize { get; set; }

        public long? UploadedByUserId { get; set; }

        public DateTime? UploadedDateTime { get; set; }
    }

}
