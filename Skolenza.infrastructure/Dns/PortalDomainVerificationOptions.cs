namespace skolenza.infrastructure.Dns;

public sealed class PortalDomainVerificationOptions
{
    public const string SectionName = "PortalDomainVerification";
    public string DnsOverHttpsEndpoint { get; set; } = string.Empty;
}
