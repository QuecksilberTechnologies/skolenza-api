using skolenza.application.DTOS.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.StaffType
{
    public class GetStaffEmploymentTypeResponseDTO: BaseRequest
    {

        public int Id { get; set; }
        public string? TypeName { get; set; }
        public string? Description { get; set; }
        public string? Remark { get; set; }
        public bool? IsActive { get; set; }
 



    }

}
