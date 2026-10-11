using System;
using System.Collections.Generic;
using System.Text;

namespace skolenza.application.DTOS.EnumDTO
{
    public class GetCurrencyRequestDTO  
    {
       public required bool IsActive { get; set; }   
    }

    public class GetAllCurrencyResponseDTO
    {
        public int Id { get; set; }
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string Symbol { get; set; } = default!;
        public bool IsActive { get; set; }
    }
}
