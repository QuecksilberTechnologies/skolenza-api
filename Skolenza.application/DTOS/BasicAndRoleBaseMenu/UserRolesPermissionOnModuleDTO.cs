using skolenza.domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.BasicAndRoleBaseMenu
{
    public class UserRolesPermissionOnModuleDTO
    {
        public int Id { get; set; }
        public string? ModuleName { get; set; }
        public string? ModuleDescription { get; set; }
        public string? ModuleURL { get; set; }
        public byte[]? ImageIcon { get; set; }
        public string? SubModuleName { get; set; }
        public string? SubModuleDescription { get; set; }
        public string? ActionType { get; set; }
        public string? ActionDescription { get; set; }
        public bool HasAccess { get; set; }
        public bool? IsActive { get; set; }
    }
}
