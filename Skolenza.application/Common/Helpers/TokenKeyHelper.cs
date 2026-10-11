using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.Common.Helpers
{
    namespace skolenza.application.Configuration
    {
        
            public static class TokenKeyHelper
            {
                public static string GetJwtSecret(IConfiguration configuration)
                {
                    try
                    {
                        var secret = configuration["JWTSettings:Secret"];
                        if (string.IsNullOrEmpty(secret))
                            throw new Exception("JWT secret key not found in configuration.");

                        return secret;
                    }
                    catch (Exception ex)
                    {
                        // optional: error logging
                        throw new Exception($"Error reading JWT secret key: {ex.Message}");
                    }
                }
            }
       
    }

}
