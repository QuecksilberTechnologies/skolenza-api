using skolenza.domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.Role
{

    public class RoleInfoDTO
    {
        public int Id { get; set; }

        public string RoleName { get; set; } = string.Empty;

        public string? Description { get; set; }

    }

}
