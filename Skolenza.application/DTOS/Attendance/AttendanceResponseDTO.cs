using skolenza.application.Constants;
using skolenza.application.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.Attendance
{
    public class AttendanceResponseDTO
    {
        public string Message { get; set; }
        public bool Status { get; set; }


    }
}
