using skolenza.domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.BasicAndRoleBaseMenu
{
    public class BasicMenuDTO
    {
        public int Id { get; set; }
        public string? MenuName { get; set; }
        public string? MenuUrl { get; set; }
        public int? ParentMenuId { get; set; }
        public string? ParentMenuName { get; set; }
        public int? ForPlatform { get; set; }
        public byte[]? ImageIcon { get; set; }
        public bool IsMenuDisplayInUi { get; set; }
        public bool IsDisplayable { get; set; }

    }



}
