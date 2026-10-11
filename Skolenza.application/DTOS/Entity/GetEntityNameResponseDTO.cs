using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.Entity
{
    public class GetEntityNameResponseDTO
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

}
