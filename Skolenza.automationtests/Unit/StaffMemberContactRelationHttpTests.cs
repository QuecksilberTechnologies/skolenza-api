using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using skolenza.api.Controllers.Staff;
using skolenza.application.DTOS.Staff.Contact;
using skolenza.application.Features.StaffMemberCmd;
using skolenza.application.Features.StaffMemberCmd.Contact.Handlers;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.ILogger;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using NUnit.Framework;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("StaffMemberContactRelation")]
[NonParallelizable]
public sealed class StaffMemberContactRelationHttpTests
{
    [TestCase("missing", HttpStatusCode.Unauthorized)]
    [TestCase("invalid", HttpStatusCode.Unauthorized)]
    [TestCase("valid", HttpStatusCode.OK)]
    public async Task Relation_endpoint_checks_only_bearer_token(string tokenCase, HttpStatusCode expected)
    {
        // Isolated local HTTP host and random key; never signs or stores a product session token.
        var key = new SymmetricSecurityKey(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true, IssuerSigningKey = key,
                ValidateIssuer = true, ValidIssuer = "contact-test",
                ValidateAudience = true, ValidAudience = "contact-test",
                ValidateLifetime = true, ClockSkew = TimeSpan.Zero
            };
        });
        builder.Services.AddAuthorization();
        builder.Services.AddControllers().AddApplicationPart(typeof(ContactController).Assembly);
        builder.Services.AddSingleton(Proxy<ILoggerService>((_, _) => null));
        var handler = new GetContactRelationOptionsQueryHandler();
        var behavior = new StaffMemberInstitutionPermissionBehavior<GetContactRelationOptionsQuery,
            ApiResponse<IReadOnlyList<ContactRelationOptionResponseDTO>>>(
                Unexpected<IUnitOfWork>(), Unexpected<ICommonRequestService>(), Unexpected<IIdEncoderService>(),
                NullLogger<StaffMemberInstitutionPermissionBehavior<GetContactRelationOptionsQuery,
                    ApiResponse<IReadOnlyList<ContactRelationOptionResponseDTO>>>>.Instance);
        var handlerCalls = 0;
        builder.Services.AddSingleton(Proxy<IMediator>((method, args) =>
        {
            Assert.That(method.Name, Is.EqualTo("Send"));
            var request = (GetContactRelationOptionsQuery)args![0]!;
            return behavior.Handle(request, async cancellation =>
            {
                handlerCalls++;
                return await handler.Handle(request, cancellation);
            }, (CancellationToken)args[1]!);
        }));
        await using var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        await app.StartAsync();
        try
        {
            var address = app.Services.GetRequiredService<IServer>().Features
                .Get<IServerAddressesFeature>()!.Addresses.Single();
            using var client = new HttpClient { BaseAddress = new Uri(address) };
            if (tokenCase != "missing")
            {
                var token = tokenCase == "invalid" ? "invalid-token" :
                    new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
                        issuer: "contact-test", audience: "contact-test",
                        expires: DateTime.UtcNow.AddMinutes(1),
                        signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            using var response = await client.GetAsync("/api/Staff/Contact/relation-options");
            Assert.That(response.StatusCode, Is.EqualTo(expected));
            Assert.That(handlerCalls, Is.EqualTo(tokenCase == "valid" ? 1 : 0));
            if (tokenCase == "valid")
            {
                using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var options = json.RootElement.GetProperty("data");
                Assert.That(options.GetArrayLength(), Is.EqualTo(17));
                Assert.That(options.EnumerateArray().Single(item => item.GetProperty("id").GetInt32() == 16)
                    .GetProperty("label").GetString(), Is.EqualTo("Owner"));
            }
        }
        finally
        {
            await app.StopAsync();
        }
    }

    private static T Unexpected<T>() where T : class => Proxy<T>((method, _) =>
        throw new AssertionException("Enum lookup must not invoke persistence/context: " + method.Name));

    private static T Proxy<T>(Func<MethodInfo, object?[]?, object?> invoke) where T : class
    {
        var proxy = DispatchProxy.Create<T, BulkImportPermissionTests.TestProxy>();
        ((BulkImportPermissionTests.TestProxy)(object)proxy).InvokeMethod = invoke;
        return proxy;
    }
}

