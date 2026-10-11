using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.Transport
{
    public class GetAllTravelModeDTO
    {
        public int? Id { get; set; } // Nullable
        public string TravelModeName { get; set; }  // Default value
        public string? Description { get; set; } // Nullable
        public bool IsActive { get; set; } = false; // Default false
        public long? AddedById { get; set; } // Nullable
        public DateTime AddedDateTime { get; set; } = DateTime.UtcNow; // Default value
        public long? UpdatedById { get; set; } // Nullable
        public DateTime UpdatedDateTime { get; set; } = DateTime.UtcNow; // Default value
    }
}
