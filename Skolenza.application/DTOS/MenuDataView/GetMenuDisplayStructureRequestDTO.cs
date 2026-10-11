using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.MenuDataView
{
    /// <summary> Get menu display structure/summary>
    public class GetMenuDisplayStructureRequestDTO
    {
     
            /// <summary> InstitutionId Required</summary>
            // ✅ Required Fields
        
            public long InstitutionId { get; set; }
            public long StaffMemberId { get; set; }
            public long RoleId { get; set; }


        
    }
}
