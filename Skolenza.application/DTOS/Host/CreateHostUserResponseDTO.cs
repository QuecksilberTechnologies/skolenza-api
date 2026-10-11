using System;
using System.Collections.Generic;
using System.Text;

namespace skolenza.application.DTOS.Host
{
    public class CreateHostUserResponseDTO
    {
        public long Id { get; set; }

        public long HostRoleId { get; set; }

        public string Name { get; set; } = null!;

        public string LoginId { get; set; } = null!;

        public string? Email { get; set; }

        public string? MobileNumber { get; set; }

        public bool IsActive { get; set; }

        public string? RoleName { get; set; }

        public List<HostUserPermissionResponseDTO> Permissions { get; set; }
            = new();
    }
}
