using skolenza.application.Common.Helpers.skolenza.application.Configuration;
using skolenza.application.Common.Helpers.EncryptionHelper;
using skolenza.application.Interfaces.IEncryptionService;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.Common.Helpers.RequestHelper
{
    public static class RequestCommonHelper
    {
        public static string? ExtractBearerToken(HttpContext? httpContext)
        {
            return httpContext?
                .Request?
                .Headers["Authorization"]
                .ToString()?
                .Replace("Bearer ", "");
        }

        public static TokenClaimsModel? ValidateAndExtractClaims(string token, IConfiguration config)
        {
            var secretKey = TokenKeyHelper.GetJwtSecret(config);
            var rawClaims = TokenClaimHelper.ExtractClaims(token, secretKey);

            if (rawClaims == null || rawClaims.IsExpired)
                return null;

            return new TokenClaimsModel
            {
                UserId = rawClaims.UserId,
                InstitutionId = rawClaims.InstitutionId,
                InstitutionEncriptionKey =  EncryptionSanitizer.SuperSanitize(rawClaims.InstitutionEncriptionKey),
                RoleId = rawClaims.RoleId,
                RoleTypeId = rawClaims.RoleTypeId,
                Expiry = rawClaims.Expiry,
                IsExpired = rawClaims.IsExpired,
                TokenPurpose = rawClaims.TokenPurpose,
                StaffMemberId = rawClaims.StaffMemberId,
                Email = rawClaims.Email



            };
        }

        public static (long UserEmpId, long InstitutionId) DecodeUserAndInstitution(
            string encodedUserId,
            string encInstitutionId,
            string institutionKey,
            IIdEncoderService encoder)
        {
            string finalKey = EncryptionSanitizer.SuperSanitize(institutionKey);

            long userId = encoder.DecodeId_long(    EncryptionSanitizer.CleanEncodedInput(encodedUserId),   finalKey );

            long institutionId = encoder.DecodeId_long(encInstitutionId, finalKey);

            return (userId, institutionId);
        }

        public static long DecodeUserAndInstitutionIds(  string encInstitutionId,
             string institutionKey,
            IIdEncoderService encoder)
        {
            string finalKey = EncryptionSanitizer.SuperSanitize(institutionKey);

            long institutionId = encoder.DecodeId_long(encInstitutionId, finalKey);

            return institutionId;
        }
    
        public static long  DecodeOnlyStaffMemberId(
           string encodedEmpId,         
           string institutionKey,
           IIdEncoderService encoder)
        {
           

                long userId = encoder.DecodeId_long(EncryptionSanitizer.CleanEncodedInput(encodedEmpId),
                institutionKey
            );

           

            return (userId);
        }

        public static string? Clean(string? value)
        {
            return EncryptionSanitizer.CleanEncodedInput(value);
        }
    }

}
