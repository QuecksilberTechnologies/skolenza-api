using skolenza.application.DTOs.BasicAndRoleBaseMenu;
using skolenza.application.DTOs.UserLogin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.Interfaces.IRepositories
{
    public interface IStaffEmploymentTypeBasicMenuRepository
    {
        //   public  Task<AccessDetailResponseDTO> GetAccessDetailResponseDTO(AccessDetailRequestDTO? accessDetailRequest);
        public   Task<IEnumerable<BasicMenuDTO>> GetBasicMenuDTO(int? staffEmploymentTypeId, int? forPlatform);
      

    }
}
