using skolenza.application.DTOS.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.Staff.Type
{
    public class GetStaffEmploymentTypeResponseDTO: BaseRequest
    {
       
        public int? StaffEmploymentTypeId { get; set; }
        public string? StaffEmploymentType { get; set; }
        public bool IsActive { get; set; }  
        public int GenderId { get ; set; }  
        public string? GenderType { get ; set; }
        public bool? IsMarried { get; set; }          
        public string? StaffMemberName { get; set; }
        public string? OfficialEmail { get; set; }
        public string? HasPermanent { get; set; }
        public DateTime? DateOfExit  { get; set; }

    }
}
