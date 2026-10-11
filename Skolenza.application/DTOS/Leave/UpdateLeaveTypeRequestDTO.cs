using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.Leave
{
    public class UpdateLeaveTypeRequestDTO
    {
        public long StaffMemberId { get; set; }
        public int RoleId { get; set; }
        public long InstitutionId { get; set; }
        public int Id { get; set; }
        public string UploadIcon { get; set; } = string.Empty;

        public string LeaveName { get; set; } = string.Empty;
        public string? Description { get; set; } // Nullable
        public bool IsActive { get; set; } = false; // Default false


        //public long? UpdatedById { get; set; } // Nullable
        //public DateTime UpdatedDateTime { get; set; } = DateTime.UtcNow; // Default value

    }
}
