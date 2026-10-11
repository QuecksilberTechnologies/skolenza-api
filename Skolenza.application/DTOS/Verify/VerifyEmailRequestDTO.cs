using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.Verify
{
    public class VerifyEmailRequestDTO
    {
        public string Token { get; set; }

    }
}
