using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.UserLogin
{
    public class ResetLoginPasswordRequestDTO
    {
        
        public string LoginId { get; set; }
        public string Otp { get; set; }
        public string Password { get; set; }
    }
}
