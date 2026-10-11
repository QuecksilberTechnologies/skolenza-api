using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.Holiday
{
    public class BasicRequestDTO : skolenza.application.DTOs.BaseDTO.PermissionRequestDTO
    {
        public long? CampusId { get; set; }

        public int? HolidayYear { get; set; }
    }
}
