using skolenza.application.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.Staff.AccessControlReadOnlyType
{
    public class StaffMemberDependentEditableFieldDTO
    {
        
            [AccessControl(ReadOnly = true)]
            public int Id { get; set; }

            [AccessControl(ReadOnly = true)]     
            public long StaffMemberId { get; set; }

            [AccessControl(ReadOnly = false)]
            public string? Relation { get; set; }

            [AccessControl(ReadOnly = false)]
            public DateTime? DateOfBirth { get; set; }

            [AccessControl(ReadOnly = false)]
            public bool? IsCoveredInPolicy { get; set; }

            [AccessControl(ReadOnly = false)]
            public bool? IsMarried { get; set; }
 
       



    }
}
