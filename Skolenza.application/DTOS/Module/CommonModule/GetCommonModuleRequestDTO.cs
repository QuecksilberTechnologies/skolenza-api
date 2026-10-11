using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.Module.CommonModule
{
    public class GetCommonModuleRequestDTO
    {
        public long StaffMemberId { get; set; }
        public int RoleId { get; set; }
        public int InstitutionId { get; set; }
        public bool IsActive { get; set; }
        public bool IsModuleDisplayInUi { get; set; }
        

       
    }
}
