using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.AssetDTO.asset
{
    public class GetAssetRequestByIdDTO
    {
        public long? InstitutionId { get; set; }
        public int RoleId { get; set; }      
        public long StaffMemberId { get; set; }
        public long Id { get; set; }
        
        
    }
}
