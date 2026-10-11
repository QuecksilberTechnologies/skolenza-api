using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.UserLogin
{
    public class NewLoginPasswordRequestDTO
    {
        public string Token { get; set; } = string.Empty;   // 🔐 Email se aaya hua token
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;

    }
}
