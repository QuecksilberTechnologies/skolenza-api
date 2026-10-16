using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.Transport
{
    public class TravelModeRequestDTO
    {
        public long Id { get; set; }
        public int SelfRoleId { get; set; }
        public int StaffPositionMasterId { get; set; }
        public int TravelModeTypeId { get; set; }

    }
}
