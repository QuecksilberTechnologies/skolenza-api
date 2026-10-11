using System;
using System.Collections.Generic;
using System.Text;

namespace skolenza.application.DTOS.Host
{
    public class CreateHostRoleResponseDTO
    {
        public long Id { get; set; }

        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public bool IsActive { get; set; }

        public List<HostRolePermissionResponseDTO> Permissions { get; set; }
            = new();
    }
}
