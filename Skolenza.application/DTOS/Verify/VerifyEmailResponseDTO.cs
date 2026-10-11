using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.Verify
{
    public class VerifyEmailResponseDTO
    {
        public long StaffMemberId { get; set; }      
        public long? InstitutionId { get; set; }      
        public string UserId { get; set; }      
        public string Expiry { get; set; }
        public bool IsExpired { get; set; }
    }
}
