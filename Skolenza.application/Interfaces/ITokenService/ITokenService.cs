using skolenza.application.Common.Helpers;
using skolenza.application.DTOs.Staff;
using skolenza.application.DTOs.UserLogin;
using skolenza.application.DTOS.Token;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

// ============================================================================
// Author      : Deepesh Gupta
// Company     : Quecksilber Technologies
// Role        : CEO
// Purpose     : Defines Institution and Host token-generation operations.
// ============================================================================

namespace skolenza.application.Interfaces.ITokenService
{
    // ITokenService.cs (Application Layer)
    /// <summary>
    /// Defines access-token and refresh-token operations used by authentication flows.
    /// </summary>
    public interface ITokenService
    {
           public Task <string> GenerateInstitutionToken(GetTokenInfoDTO dto);
           public Task<string> GenerateHostToken(HostTokenInfoDTO dto);
           public Task<string> GenerateStudentPortalToken(StudentPortalTokenInfoDTO dto);
       
           bool ValidateToken(string token);
        public Task<string>  GenerateRefreshToken();
        // ✅ Naye methods for extracting info
        Task<string> GetUserInfoFromToken(string token);
        DateTime? GetExpiryFromToken(string token);
        Task<GetTokenInfoDTO?> GetUserInfoByLoginIdAsync(string loginId);
        public TokenClaimsModel? ValidateAndExtractClaims(string token);
    }


}
