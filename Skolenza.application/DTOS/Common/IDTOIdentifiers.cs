using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.Common
{
    public class IDTOIdentifiers
    {
        public string EncriptedStaffMemberId { get; set; } = null!;
        public string? EncriptedId { get; set; } = null!;
        public long StaffMemberId { get; set; } = 0;
        public int Id { get; set; } = 0;
        public long InstitutionId { get; set; } = 0;
        public string? encryptedInstitutionId { get; set; } = null!;
    }
}
