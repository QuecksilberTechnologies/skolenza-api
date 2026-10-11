using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.Common.Helpers
{
    public class TokenClaimsModel
    {
        public string? UserId { get; set; }
        public string? InstitutionId { get; set; }
        public string? InstitutionEncriptionKey { get; set; }
        public string? RoleId { get; set; }
        public string? RoleTypeId { get; set; }
        public DateTime? Expiry { get; set; }
        public bool IsExpired { get; set; }
        // 🔥 ADD THESE
        public string? StaffMemberId { get; set; }
        public string? TokenPurpose { get; set; }
        public string? UserType { get; set; }
        public string? Email { get; set; }
        



    }




}
