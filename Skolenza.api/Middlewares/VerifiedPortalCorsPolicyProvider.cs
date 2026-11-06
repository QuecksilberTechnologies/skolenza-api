using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;
using skolenza.application.Common.Helpers;
using skolenza.application.Constants;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces.IRepositories;

namespace skolenza.api.Middlewares;

/// <summary>Extends the existing frontend policy only for verified HTTPS portal origins.</summary>
public sealed class VerifiedPortalCorsPolicyProvider(
    IOptions<CorsOptions> options,
    ILogger<VerifiedPortalCorsPolicyProvider> logger) : ICorsPolicyProvider
{
    public const string FrontendPolicyName = "AllowFrontend";
    private readonly DefaultCorsPolicyProvider defaultProvider = new(options);

    public async Task<CorsPolicy?> GetPolicyAsync(HttpContext context, string? policyName)
    {
        var policy = await defaultProvider.GetPolicyAsync(context, policyName);
        if (policy is null || !string.Equals(policyName, FrontendPolicyName, StringComparison.Ordinal) ||
            (!context.Request.Path.StartsWithSegments(StudentConstants.PortalAuthenticationApiPath) &&
             !context.Request.Path.StartsWithSegments(StudentConstants.PortalSelfServiceApiPath)))
        {
            return policy;
        }

        var origin = context.Request.Headers.Origin.ToString();
        if (policy.Origins.Any(item => string.Equals(item, origin, StringComparison.OrdinalIgnoreCase)))
        {
            return policy;
        }

        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps || !uri.IsDefaultPort ||
            !string.IsNullOrEmpty(uri.UserInfo) || uri.AbsolutePath != "/" ||
            !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment) ||
            !string.Equals(origin, uri.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase))
        {
            return policy;
        }

        string hostname;
        try
        {
            hostname = InstitutionPortalDomainChallenge.NormalizeDomain(uri.Host);
        }
        catch (ValidationErrorException)
        {
            return policy;
        }

        if (!string.Equals(hostname, uri.IdnHost, StringComparison.OrdinalIgnoreCase))
        {
            return policy;
        }

        try
        {
            var repository = context.RequestServices.GetRequiredService<IStudentPortalAuthRepository>();
            var institutionId = await repository.ResolveVerifiedInstitutionAsync(hostname, context.RequestAborted);
            if (!institutionId.HasValue || institutionId.Value <= 0)
            {
                return policy;
            }
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Unable to resolve a verified portal CORS origin.");
            return policy;
        }

        return new CorsPolicyBuilder()
            .WithOrigins(origin)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .Build();
    }
}
