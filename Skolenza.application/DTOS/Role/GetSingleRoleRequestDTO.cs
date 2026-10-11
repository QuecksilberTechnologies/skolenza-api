using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.Role
{
    public class GetSingleRoleRequestDTO
    {      
        public required int Id { get; set; }
    }
}
