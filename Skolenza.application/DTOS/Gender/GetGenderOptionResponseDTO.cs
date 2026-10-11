using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.Gender
{
    public class GetGenderOptionResponseDTO
    {
        public int Id { get; set; }
        public string GenderName { get; set; } = null!;
    }
}
