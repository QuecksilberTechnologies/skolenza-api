using System;
using System.Collections.Generic;
using System.Text;

namespace skolenza.application.DTOS.Host
{
    public class CreateHostUserRequestDTO
    {
        
        public required long HostRoleId { get; set; } 

        public string Name { get; set; } = null!;

        public string LoginId { get; set; } = null!;

        public string Password { get; set; } = null!;

        public string? Email { get; set; }

        public string? MobileNumber { get; set; }
        public bool  IsActive { get; set; } = true; 
    }

}
