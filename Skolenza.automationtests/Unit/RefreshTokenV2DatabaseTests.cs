using skolenza.application;
using skolenza.application.Common.Enums;
using skolenza.application.Common.Helpers.Hash;
using skolenza.application.DTOS.Token;
using skolenza.application.DTOS.Token.ems.application.DTOs.UserLogin;
using skolenza.application.Features.UserLoginAndDashboardCmd.Handlers;
using skolenza.domain.Entity;
using skolenza.infrastructure;
using skolenza.persistance;
using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using NUnit.Framework;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MediatR;
using skolenza.application.Interfaces.ILogger;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.ITokenService;
using System.Reflection;

namespace skolenza.automationtests.Unit;

[TestFixture, Category("RefreshV2Database"), NonParallelizable]
public sealed class RefreshTokenV2DatabaseTests
{
    [TestCase(false)]
    [TestCase(true)]
    public async Task Active_owner_refresh_rotates_tokens_with_proxy_chain_IP(bool host)
    {
        await RunRefreshAsync(host, false);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Http_legacy_and_v2_routes_accept_each_others_replacement_tokens(bool host)
    {
        await RunRefreshAsync(host, true);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Database_write_failure_rolls_back_revocation(bool host)
    {
        await RunRefreshAsync(host, false, "write-failure");
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Concurrent_same_token_must_have_only_one_winner(bool host)
    {
        await RunRefreshAsync(host, false, "concurrent");
    }

    private static async Task RunRefreshAsync(bool host, bool http, string? scenario = null)
    {
        var connection = Environment.GetEnvironmentVariable("SKOLENZA_BULK_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(connection))
        {
            Assert.Ignore("Requires the isolated local skolenza_bulk_test fixture.");
        }
        var parsed = new NpgsqlConnectionStringBuilder(connection!);
        Assert.That(parsed.Database, Is.EqualTo("skolenza_bulk_test"));
        Assert.That(parsed.Host, Is.AnyOf("localhost", "127.0.0.1"));
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
        {
            directory = directory.Parent;
        }
        Assert.That(directory, Is.Not.Null);
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(directory!.FullName, "skolenza.api", "appsettings.json"))
            .AddJsonFile(Path.Combine(directory.FullName, "skolenza.api", "appsettings.Development.json"), optional: true)
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connection
            }).Build();
        await using var db = new SkolenzaDbContext(new DbContextOptionsBuilder<SkolenzaDbContext>().UseNpgsql(connection).Options);
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddApplication();
        services.AddPersistence(configuration);
        services.AddInfrastructure(configuration);
        // Use the quiet local test context; no sensitive SQL parameter logging or hosted workers.
        services.AddScoped(_ => new SkolenzaDbContext(
            new DbContextOptionsBuilder<SkolenzaDbContext>().UseNpgsql(connection).Options));
        services.AddTransient<RefreshTokenV2CommandHandler>();
        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var raw = Guid.NewGuid().ToString("N");
        var token = new RefreshToken
        {
            Token = HashHelper.Sha256(raw), CreatedAt = DateTime.UtcNow, ExpiryDate = DateTime.UtcNow.AddDays(1),
            IsRevoked = false, CreatedByIp = "127.0.0.1"
        };
        if (host)
        {
            var owner = await db.Set<HostUser>().FirstAsync(x => x.IsActive && !x.IsSoftDeleted);
            token.HostUserId = owner.Id;
            token.LoginId = owner.LoginId;
            token.UserType = (short)LoginUserType.Host;
        }
        else
        {
            var owner = await db.Set<LoginCredential>().FirstAsync(x => x.IsActive && x.IsSoftDeleted != true);
            token.LoginCredentialId = owner.Id;
            token.LoginId = owner.LoginId;
            token.UserType = (short)LoginUserType.InstitutionStaffMember;
        }
        db.Add(token);
        await db.SaveChangesAsync();
        var marker = "v2-test-" + Guid.NewGuid().ToString("N");
        try
        {
            var handler = scope.ServiceProvider.GetRequiredService<RefreshTokenV2CommandHandler>();
            if (scenario == "write-failure")
            {
                // Force replacement rejection after the real PostgreSQL conditional claim.
                var service = scope.ServiceProvider;
                var real = service.GetRequiredService<IRefreshTokenRepository>();
                var failing = DispatchProxy.Create<IRefreshTokenRepository, BulkImportPermissionTests.TestProxy>();
                ((BulkImportPermissionTests.TestProxy)(object)failing).InvokeMethod = (method, args) =>
                    method.Name == "InsertAsync" ? Task.FromResult(false) : method.Invoke(real, args);
                handler = new RefreshTokenV2CommandHandler(service.GetRequiredService<IUnitOfWork>(),
                    service.GetRequiredService<ITokenService>(), failing, service.GetRequiredService<IIdEncoderService>(),
                    service.GetRequiredService<ILogger<RefreshTokenV2CommandHandler>>());
                Assert.ThrowsAsync<InvalidOperationException>(async () => await handler.Handle(
                    new RefreshTokenV2Command(new RefreshTokenRequestDTO
                    {
                        RefreshToken = raw,
                        IpAddress = "127.0.0.1"
                    }), default));
                var original = await db.RefreshTokens.AsNoTracking().SingleAsync(x => x.Id == token.Id);
                Assert.That(original.IsRevoked, Is.False);
                Assert.That(original.ReplacedByToken, Is.Null);
                return;
            }
            if (scenario == "concurrent")
            {
                await VerifyConcurrencyAsync(provider, raw, marker);
                return;
            }
            Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await handler.Handle(
                new RefreshTokenV2Command(new RefreshTokenRequestDTO { RefreshToken = "invalid-fixture-token" }), default));
            var timer = System.Diagnostics.Stopwatch.StartNew();
            var result = await handler.Handle(new RefreshTokenV2Command(new RefreshTokenRequestDTO
            {
                RefreshToken = raw, IpAddress = "104.28.157.140, 172.71.198.92, 10.199.43.1"
            }), default);
            timer.Stop();
            TestContext.WriteLine($"V2 {(host ? "Host" : "Institution")} handler elapsed: {timer.ElapsedMilliseconds} ms (local, not production SLA).");
            Assert.That(result.IsSucceeded, Is.True, result.Message);
            Assert.That(result.Data!.RefreshToken, Is.Not.Empty);
            var saved = await db.RefreshTokens.AsNoTracking().SingleAsync(x => x.Id == token.Id);
            Assert.That(saved.IsRevoked, Is.True);
            Assert.That(saved.ReplacedByToken, Is.EqualTo(HashHelper.Sha256(result.Data.RefreshToken!)));
            Assert.That(result.Data.Token, Is.Not.Empty);
            var replacement = await db.RefreshTokens.AsNoTracking().SingleAsync(x => x.Token == saved.ReplacedByToken);
            Assert.That(result.Data.RefreshTokenExpiresAtUtc, Is.EqualTo(replacement.ExpiryDate).Within(TimeSpan.FromMilliseconds(1)));
            var jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(result.Data.Token);
            Assert.That(result.Data.TokenExpiry, Is.EqualTo(jwt.ValidTo));
            Assert.That(replacement.HostUserId, Is.EqualTo(token.HostUserId));
            Assert.That(replacement.LoginCredentialId, Is.EqualTo(token.LoginCredentialId));
            Assert.That(replacement.UserType, Is.EqualTo(token.UserType));
            Assert.That(replacement.Token, Is.Not.EqualTo(result.Data.RefreshToken));
            var json = System.Text.Json.JsonSerializer.SerializeToElement(result.Data,
                new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
            Assert.That(json.EnumerateObject().Select(x => x.Name), Is.EquivalentTo(
                new[] { "token", "refreshToken", "tokenExpiry", "refreshTokenExpiresAtUtc" }));
            Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await handler.Handle(
                new RefreshTokenV2Command(new RefreshTokenRequestDTO { RefreshToken = raw }), default));
            if (http)
            {
                await VerifyHttpAsync(scope.ServiceProvider, result.Data.RefreshToken);
            }
        }
        finally
        {
            db.ChangeTracker.Clear();
            var current = await db.RefreshTokens.AsNoTracking().SingleAsync(x => x.Id == token.Id);
            var ids = new List<long>();
            while (current != null)
            {
                ids.Add(current.Id);
                current = current.ReplacedByToken == null ? null : await db.RefreshTokens.AsNoTracking()
                    .SingleOrDefaultAsync(x => x.Token == current.ReplacedByToken);
            }
            await db.RefreshTokens.Where(x => ids.Contains(x.Id)).ExecuteDeleteAsync();
            await db.RefreshTokens.Where(x => x.CreatedByIp == marker).ExecuteDeleteAsync();
        }
    }

    private static async Task VerifyConcurrencyAsync(IServiceProvider provider, string raw, string marker)
    {
        var reads = 0;
        var bothRead = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<bool> AttemptAsync()
        {
            using var scope = provider.CreateScope();
            var service = scope.ServiceProvider;
            var real = service.GetRequiredService<IRefreshTokenRepository>();
            async Task<RefreshToken?> ReadAsync(string hash)
            {
                var row = await real.GetByHashedTokenAsync(hash);
                if (Interlocked.Increment(ref reads) == 2)
                {
                    bothRead.TrySetResult();
                }
                await bothRead.Task.WaitAsync(TimeSpan.FromSeconds(30));
                return row;
            }
            var proxy = DispatchProxy.Create<IRefreshTokenRepository, BulkImportPermissionTests.TestProxy>();
            ((BulkImportPermissionTests.TestProxy)(object)proxy).InvokeMethod = (method, args) =>
                method.Name == "GetByHashedTokenAsync" ? ReadAsync((string)args![0]!) : method.Invoke(real, args);
            var handler = new RefreshTokenV2CommandHandler(service.GetRequiredService<IUnitOfWork>(),
                service.GetRequiredService<ITokenService>(), proxy, service.GetRequiredService<IIdEncoderService>(),
                service.GetRequiredService<ILogger<RefreshTokenV2CommandHandler>>());
            try
            {
                return (await handler.Handle(new RefreshTokenV2Command(new RefreshTokenRequestDTO
                {
                    RefreshToken = raw,
                    IpAddress = marker
                }), default)).IsSucceeded;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }
        var results = await Task.WhenAll(AttemptAsync(), AttemptAsync());
        Assert.That(results.Count(x => x), Is.EqualTo(1),
            "One consumed refresh token must not issue two successful replacement sessions.");
    }

    private static async Task VerifyHttpAsync(IServiceProvider services, string refreshToken)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton(services.GetRequiredService<IMediator>());
        builder.Services.AddSingleton<ILoggerService, QuietLogger>();
        builder.Services.AddControllers().AddApplicationPart(typeof(skolenza.api.Controllers.Login.AuthController).Assembly);
        builder.Services.AddAuthorization();
        await using var app = builder.Build();
        app.UseMiddleware<skolenza.api.Middlewares.ErrorHandlerMiddleware>();
        app.UseAuthorization();
        app.MapControllers();
        await app.StartAsync();
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
        // Exercise opt-in and rollback with current replacement tokens, never the consumed token.
        foreach (var route in new[] { "refresh-token", "refresh-token-v2", "refresh-token" })
        {
            using var response = await client.PostAsJsonAsync("/api/Auth/" + route,
                new RefreshTokenRequestDTO { RefreshToken = refreshToken });
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.That(payload.RootElement.GetProperty("isSucceeded").GetBoolean(), Is.True);
            var data = payload.RootElement.GetProperty("data");
            refreshToken = data.GetProperty("refreshToken").GetString()!;
            Assert.That(data.GetProperty("token").GetString(), Is.Not.Empty);
            if (route == "refresh-token-v2")
            {
                Assert.That(data.EnumerateObject().Count(), Is.EqualTo(4));
            }
            else
            {
                Assert.That(data.TryGetProperty("success", out _), Is.True);
            }
        }
        using var invalid = await client.PostAsJsonAsync("/api/Auth/refresh-token-v2",
            new RefreshTokenRequestDTO { RefreshToken = "unknown-fixture" });
        Assert.That(invalid.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        using var blank = await client.PostAsJsonAsync("/api/Auth/refresh-token-v2",
            new RefreshTokenRequestDTO { RefreshToken = " " });
        Assert.That(blank.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        using var oversized = await client.PostAsJsonAsync("/api/Auth/refresh-token-v2",
            new RefreshTokenRequestDTO { RefreshToken = refreshToken, IpAddress = new string('1', 51) });
        Assert.That(oversized.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        using var boundary = await client.PostAsJsonAsync("/api/Auth/refresh-token-v2",
            new RefreshTokenRequestDTO { RefreshToken = refreshToken, IpAddress = new string('1', 50) });
        Assert.That(boundary.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        await app.StopAsync();
    }

    private sealed class QuietLogger : ILoggerService
    {
        public void LogInfo(string message) { }
        public void LogWarn(string message) { }
        public void LogDebug(string message) { }
        public void LogError(string message) { }
    }
}
