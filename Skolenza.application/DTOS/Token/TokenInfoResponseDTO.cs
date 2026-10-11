using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.Token
{
    public class TokenInfoResponseDTO
    {
        public string? Token { get; set; }
        public string? RefreshToken { get; set; }


    }
}
