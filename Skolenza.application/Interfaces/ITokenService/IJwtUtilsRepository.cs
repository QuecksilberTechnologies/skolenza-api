using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.Interfaces.ITokenService
{
    public interface IJwtUtilsRepository
    {
       // IDictionary<string, string> ValidateToken(string token, string secretKey);
        string GetEmailFromToken(string token);
    }
}
