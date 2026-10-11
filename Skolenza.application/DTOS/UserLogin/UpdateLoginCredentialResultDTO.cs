using System;
using System.Collections.Generic;
using System.Text;

namespace skolenza.application.DTOS.UserLogin
{
    public class UpdateLoginCredentialResultDTO
    {
        public int Status { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
