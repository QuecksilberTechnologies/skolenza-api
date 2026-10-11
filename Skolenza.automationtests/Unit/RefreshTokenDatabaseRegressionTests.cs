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

namespace skolenza.automationtests.Unit;

[TestFixture, Category("RefreshDatabase"), NonParallelizable]
public sealed class RefreshTokenDatabaseRegressionTests
{
    [TestCase(false)]
    [TestCase(true)]
    public async Task Active_owner_refresh_rotates_tokens_with_proxy_chain_IP(bool host)
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
        services.AddScoped(_ => db);
        services.AddTransient<RefreshTokenCommandHandler>();
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
        try
        {
            var handler = scope.ServiceProvider.GetRequiredService<RefreshTokenCommandHandler>();
            Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await handler.Handle(
                new RefreshTokenCommand(new RefreshTokenRequestDTO { RefreshToken = "invalid-fixture-token" }), default));
            var result = await handler.Handle(new RefreshTokenCommand(new RefreshTokenRequestDTO
            {
                RefreshToken = raw, IpAddress = "104.28.157.140, 172.71.198.92, 10.199.43.1"
            }), default);
            Assert.That(result.IsSucceeded, Is.True, result.Message);
            Assert.That(result.Data!.RefreshToken, Is.Not.Empty);
            var saved = await db.RefreshTokens.AsNoTracking().SingleAsync(x => x.Id == token.Id);
            Assert.That(saved.IsRevoked, Is.True);
            Assert.That(saved.ReplacedByToken, Is.EqualTo(HashHelper.Sha256(result.Data.RefreshToken!)));
            Assert.That(result.Data.Token, Is.Not.Empty);
            Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await handler.Handle(
                new RefreshTokenCommand(new RefreshTokenRequestDTO { RefreshToken = raw }), default));
        }
        finally
        {
            db.ChangeTracker.Clear();
            var saved = await db.RefreshTokens.AsNoTracking().SingleAsync(x => x.Id == token.Id);
            await db.RefreshTokens.Where(x => x.Id == token.Id || x.Token == saved.ReplacedByToken).ExecuteDeleteAsync();
        }
    }
}
