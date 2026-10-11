using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.UserLogin
{
    public class UpdatePasswordResponseDTO
    {
        public bool Success { get; set; }
        //public string? Token { get; set; }
        //public string? RefreshToken { get; set; }
        //public string? Message { get; set; }
    }
}
