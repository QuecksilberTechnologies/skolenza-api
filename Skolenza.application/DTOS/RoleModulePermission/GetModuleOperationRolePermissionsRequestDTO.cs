using skolenza.application.DTOS.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.RoleModulePermission
{
    public class GetInstitutionModuleOperationRolePermissionsRequestDTO : BaseRequest
    {
             public  long InstitutionId { get; set; }
             public  long StaffMemberId { get; set; }
             public required  int RoleId { get; set; }
             public  int? ModuleId { get; set; }
             public  int?  ParentModuleId { get; set; }       
             public  int? SubscriptionPlanId { get; set; }       
       

    }
   


}
