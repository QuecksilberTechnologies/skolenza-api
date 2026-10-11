using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.Role
{
    public class GetRoleSummaryResponseDTO
    {
        public long InstitutionId { get; set; }
        public bool IsActive { get; set; }
        public string RoleType { get; set; } = string.Empty;
        public string RoleCode { get; set; } = string.Empty;
        public bool IsSystemDefault { get; set; }
    }
}
