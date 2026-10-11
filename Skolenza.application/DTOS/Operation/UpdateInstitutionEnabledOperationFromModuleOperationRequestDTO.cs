using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.Operation
{
    public class UpdateInstitutionEnabledOperationFromModuleOperationRequestDTO
    {
        public long? InstitutionId { get; set; }
    }


}
