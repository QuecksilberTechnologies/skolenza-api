using System.Security.Cryptography;
using System.Text;

namespace skolenza.application.Common.Helpers;

public static class StudentPortalTokenHelper
{
    public static string CreateOpaqueToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
}
