 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.Registration
{
    public class CandidateResponseDTO
    {

        public bool Success { get; set; }  // Operation success status (true/false)
        public long? CandidateId { get; set; }  // CandidateId if success, else null
        public long? InstitutionId { get; set; }  // CandidateId if success, else null
        public string Message { get; set; }  // Success/Failure message
                                             //   public CandidateInfoDTO CandidateInfoDTO { get; set; }  // StaffMember Information

    }
}
