using skolenza.application.Common.Helpers;
using skolenza.application.Interfaces.IRepositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.Common.Models.Security
{
    public class CommonDecodedResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public long LoggedInStaffMemberId { get; set; }
        public long UserStaffMemberId { get; set; }
        public long InstitutionId { get; set; }
        public int RoleId { get; set; }
        public int RoleTypeId { get; set; }

        
        public TokenClaimsModel? Claims { get; set; }
    }
}
