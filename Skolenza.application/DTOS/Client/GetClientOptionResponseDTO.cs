using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.Client
{
    public class GetClientOptionResponseDTO
    {
        public string Id { get; set; } //  
        public string ClientName { get; set; }
    }

}
