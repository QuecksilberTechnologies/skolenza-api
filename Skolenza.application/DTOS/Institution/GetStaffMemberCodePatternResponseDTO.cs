using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; 
using skolenza.domain.Entity; 
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace skolenza.application.DTOS.Institution
{
    [Keyless]
    public class GetStaffMemberCodePatternResponseDTO
    {
        public long Id { get; set; }
        public long InstitutionId { get; set; }
        public string? Prefix { get; set; }
        public bool IncludeYear { get; set; }
        public bool IncludeMonth { get; set; }
        public bool IncludeDepartment { get; set; }
        public string Separator { get; set; } = "/";
        public int RunningNumberLength { get; set; }
        public int LastUsedNumber { get; set; }

        public bool IsActive { get; set; }

        public long AddedById { get; set; }
        public DateTime AddedDateTime { get; set; }  

        public long? UpdatedById { get; set; } 
        public DateTime? UpdatedDateTime { get; set; }  
    }

}
