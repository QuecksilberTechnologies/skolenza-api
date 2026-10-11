// ================================================================
// Purpose : Centralizes the opaque bearer-token rules for HTTPS device polling.
// ================================================================

using System.Security.Cryptography;
using System.Text;

namespace skolenza.application.Constants;

/// <summary>
/// Generates and verifies the opaque route token used by a device when it polls
/// Skolenza over HTTPS. Only a SHA-256 hash is stored in the database.
/// </summary>
public static class DeviceHttpsGatewaySecurity
{
    /// <summary>The fixed, non-secret route prefix configured on the device.</summary>
    public const string RoutePrefix = "/device-gateway";

    /// <summary>Returns a 256-bit URL-safe bearer token for one physical device.</summary>
    public static string GenerateIngressToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();

    /// <summary>Returns the durable lookup value for an ingress token.</summary>
    public static string HashIngressToken(string ingressToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(ingressToken))).ToLowerInvariant();

    /// <summary>Accepts only the fixed-width hexadecimal representation generated above.</summary>
    public static bool IsValidIngressToken(string? ingressToken) =>
        !string.IsNullOrWhiteSpace(ingressToken) &&
        ingressToken.Length == 64 &&
        ingressToken.All(character => char.IsAsciiHexDigit(character));

    /// <summary>Normalizes the configured, non-secret gateway path.</summary>
    public static bool IsValidGatewayPath(string? path) =>
        string.Equals(path?.TrimEnd('/'), RoutePrefix, StringComparison.Ordinal);
}
