using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.UserLogin
{
    public class GetNewPasswordLinkResponseDTO
    {
         
        public string UrlLink { get; set; } = string.Empty;
  

    }
}
