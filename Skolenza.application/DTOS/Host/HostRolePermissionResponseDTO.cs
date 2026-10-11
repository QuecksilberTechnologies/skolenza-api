using System;
using System.Collections.Generic;
using System.Text;

namespace skolenza.application.DTOS.Host
{
    public class HostRolePermissionResponseDTO
    {
        public int ModuleId { get; set; }

        public string? ModuleName { get; set; }

        public string? DisplayName { get; set; }

        public int OperationId { get; set; }

        public string? OperationName { get; set; }
    }
}
