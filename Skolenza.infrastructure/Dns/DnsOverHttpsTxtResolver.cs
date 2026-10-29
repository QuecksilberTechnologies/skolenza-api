using System.Net.Http.Json;
using System.Text.Json;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using Microsoft.Extensions.Options;

namespace skolenza.infrastructure.Dns;

public sealed class DnsOverHttpsTxtResolver(
    HttpClient httpClient,
    IOptions<PortalDomainVerificationOptions> options) : IDnsTxtResolver
{
    public async Task<IReadOnlyList<string>> ResolveAsync(
        string recordName,
        CancellationToken cancellationToken)
    {
        var uri = new UriBuilder(options.Value.DnsOverHttpsEndpoint)
        {
            Query = $"name={Uri.EscapeDataString(recordName)}&type=TXT"
        }.Uri;

        using var response = await httpClient.GetAsync(uri, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException("The DNS-over-HTTPS resolver returned an unavailable response.", 503);
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        if (!json.RootElement.TryGetProperty("Answer", out var answers) || answers.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return answers.EnumerateArray()
            .Where(answer => answer.TryGetProperty("type", out var type) && type.GetInt32() == 16)
            .Select(answer => answer.TryGetProperty("data", out var data) ? data.GetString() : null)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Cast<string>()
            .ToArray();
    }
}
