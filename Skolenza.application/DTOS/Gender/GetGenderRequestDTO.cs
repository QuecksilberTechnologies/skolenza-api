using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.Gender
{
    public class GetGenderRequestDTO
    {
   
        public long? InstitutionId { get; set; }                  // Required
        public long? StaffMemberId { get; set; }                  // Required
        public int? RoleId { get; set; }                   
    }
}
