using skolenza.application.DTOS.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.TicketDTO.Classification
{
    public class GetClassificationResponseDTO  
    {

        public int Id { get; set; } =0; 
        public string ClassificationName { get; set; } = null!;

        public string? Description { get; set; }

        public bool IsActive { get; set; }

        
        

        

        // ✅ Add these two lines
     

    }
}
