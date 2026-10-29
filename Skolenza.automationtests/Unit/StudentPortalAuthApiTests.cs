using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using skolenza.application.Common.Helpers;
using skolenza.api.Controllers.Student;
using skolenza.application.Common.Enums;
using skolenza.application.Constants;
using skolenza.application.DTOS.Student;
using skolenza.application.Features.StudentPortalAuthCmd;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("StudentPortalAuth")]
public sealed class StudentPortalAuthApiTests
{
    [Test]
    public void Portal_login_and_refresh_are_anonymous_but_logout_requires_portal_policy()
    {
        var controller = typeof(StudentPortalAuthController);
        var route = controller.GetCustomAttribute<Microsoft.AspNetCore.Mvc.RouteAttribute>();
        var login = controller.GetMethod(nameof(StudentPortalAuthController.Login));
        var refresh = controller.GetMethod(nameof(StudentPortalAuthController.Refresh));
        var logout = controller.GetMethod(nameof(StudentPortalAuthController.Logout));
        var profile = controller.GetMethod(nameof(StudentPortalAuthController.GetProfile));
        var acceptInvitation = controller.GetMethod(nameof(StudentPortalAuthController.AcceptInvitation));
        var claimInvitation = controller.GetMethod(nameof(StudentPortalAuthController.ClaimInvitation));
        var requestPasswordReset = controller.GetMethod(nameof(StudentPortalAuthController.RequestPasswordReset));
        var completePasswordReset = controller.GetMethod(nameof(StudentPortalAuthController.CompletePasswordReset));

        Assert.Multiple(() =>
        {
            Assert.That(route?.Template, Is.EqualTo("api/Student/portal-auth"));
            Assert.That(login?.GetCustomAttribute<AllowAnonymousAttribute>(), Is.Not.Null);
            Assert.That(refresh?.GetCustomAttribute<AllowAnonymousAttribute>(), Is.Not.Null);
            Assert.That(logout?.GetCustomAttribute<AuthorizeAttribute>()?.Policy,
                Is.EqualTo(StudentConstants.PortalAuthenticationPolicy));
            Assert.That(profile?.GetCustomAttribute<AuthorizeAttribute>()?.Policy,
                Is.EqualTo(StudentConstants.PortalAuthenticationPolicy));
            Assert.That(acceptInvitation?.GetCustomAttribute<AllowAnonymousAttribute>(), Is.Not.Null);
            Assert.That(claimInvitation?.GetCustomAttribute<AuthorizeAttribute>()?.Policy,
                Is.EqualTo(StudentConstants.PortalAuthenticationPolicy));
            Assert.That(requestPasswordReset?.GetCustomAttribute<AllowAnonymousAttribute>(), Is.Not.Null);
            Assert.That(completePasswordReset?.GetCustomAttribute<AllowAnonymousAttribute>(), Is.Not.Null);
            Assert.That(controller.GetMethods().SelectMany(method => method
                    .GetCustomAttributes(typeof(HttpMethodAttribute), true)
                    .Cast<HttpMethodAttribute>()
                    .Select(attribute => attribute.Template)),
                Is.EquivalentTo(["login", "refresh", "logout", "me", "invitations/accept", "invitations/claim",
                    "password-reset/request", "password-reset/complete"]));
        });
    }

    [Test]
    public void Login_rejects_plaintext_credential_hashes_before_password_service_fallback()
    {
        var check = typeof(StudentPortalLoginCommandHandler)
            .GetMethod("IsIdentityPasswordHash", BindingFlags.NonPublic | BindingFlags.Static);

        Assert.Multiple(() =>
        {
            Assert.That(check?.Invoke(null, ["plain-text-password"]), Is.EqualTo(false));
            Assert.That(check?.Invoke(null, ["AQAAAAIAAYagAAAAEidentity-hash"]), Is.EqualTo(true));
            Assert.That(check?.Invoke(null, [null]), Is.EqualTo(false));
        });
    }

    [Test]
    public void Portal_accounts_and_tokens_have_separate_owner_and_hash_indexes()
    {
        using var context = new SkolenzaDbContext(
            new DbContextOptionsBuilder<SkolenzaDbContext>()
                .UseNpgsql("Host=localhost;Database=skolenza_portal_model_validation;Username=postgres;Password=not-used")
                .Options);

        var account = context.Model.FindEntityType(typeof(StudentPortalAccount));
        var guardianLink = context.Model.FindEntityType(typeof(StudentPortalAccountGuardian));
        var studentLink = context.Model.FindEntityType(typeof(StudentPortalAccountStudent));
        var refresh = context.Model.FindEntityType(typeof(StudentPortalRefreshToken));
        var oneTime = context.Model.FindEntityType(typeof(StudentPortalOneTimeToken));

        Assert.Multiple(() =>
        {
            Assert.That(account?.GetTableName(), Is.EqualTo("StudentPortalAccount"));
            Assert.That(account?.GetIndexes().Any(index => index.IsUnique &&
                index.Properties.Select(property => property.Name).SequenceEqual(
                    [nameof(StudentPortalAccount.InstitutionId), nameof(StudentPortalAccount.NormalizedEmail)])), Is.True);
            Assert.That(guardianLink?.GetTableName(), Is.EqualTo("StudentPortalAccountGuardian"));
            Assert.That(studentLink?.GetTableName(), Is.EqualTo("StudentPortalAccountStudent"));
            Assert.That(refresh?.GetTableName(), Is.EqualTo("StudentPortalRefreshToken"));
            Assert.That(refresh?.GetIndexes().Any(index => index.IsUnique &&
                index.Properties.Select(property => property.Name).SequenceEqual([nameof(StudentPortalRefreshToken.TokenHash)])), Is.True);
            Assert.That(oneTime?.GetTableName(), Is.EqualTo("StudentPortalOneTimeToken"));
            Assert.That(oneTime?.GetIndexes().Any(index => index.IsUnique &&
                index.Properties.Select(property => property.Name).SequenceEqual([nameof(StudentPortalOneTimeToken.TokenHash)])), Is.True);
        });
    }

    [Test]
    public void Portal_lockout_and_token_lifetimes_are_centralized_and_enums_are_listable()
    {
        Assert.Multiple(() =>
        {
            Assert.That(StudentConstants.PortalLoginFailureLimit, Is.EqualTo(5));
            Assert.That(StudentConstants.PortalLoginLockoutMinutes, Is.EqualTo(15));
            Assert.That(StudentConstants.PortalRefreshTokenExpiryDays, Is.EqualTo(14));
            Assert.That(Enum.GetValues<StudentPortalPersonaType>(), Has.Length.EqualTo(2));
            Assert.That(Enum.GetValues<StudentPortalOneTimeTokenPurpose>(), Has.Length.EqualTo(2));
            Assert.That(ConstantValues.IsSupportedEmailTemplateCode(StudentConstants.PortalInvitationTemplateCode), Is.True);
            Assert.That(ConstantValues.IsSupportedEmailTemplateCode(StudentConstants.PortalPasswordResetTemplateCode), Is.True);
        });
    }

    [Test]
    public void Portal_one_time_tokens_are_random_opaque_and_stored_as_sha256_hashes()
    {
        var first = StudentPortalTokenHelper.CreateOpaqueToken();
        var second = StudentPortalTokenHelper.CreateOpaqueToken();
        var hash = StudentPortalTokenHelper.HashToken(first);

        Assert.Multiple(() =>
        {
            Assert.That(first, Has.Length.EqualTo(43));
            Assert.That(first, Does.Match("^[A-Za-z0-9_-]+$"));
            Assert.That(second, Is.Not.EqualTo(first));
            Assert.That(hash, Has.Length.EqualTo(64));
            Assert.That(hash, Does.Match("^[a-f0-9]{64}$"));
            Assert.That(hash, Is.Not.EqualTo(first));
            Assert.That(StudentPortalTokenHelper.HashToken(first), Is.EqualTo(hash));
        });
    }
}
