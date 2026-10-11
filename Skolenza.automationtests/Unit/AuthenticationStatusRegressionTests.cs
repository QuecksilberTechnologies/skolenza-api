using skolenza.api.Middlewares;
using skolenza.application.Common.Helpers;
using skolenza.application.DTOS.RoleModulePermission;
using skolenza.persistance.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using skolenza.persistance.Data.Context;
using System.Text.Json;

namespace skolenza.automationtests.Unit;

[TestFixture, Category("AuthenticationStatus")]
public sealed class AuthenticationStatusRegressionTests
{
    [Test]
    public async Task Unexpected_failure_returns_safe_message_and_request_identifier()
    {
        const string requestId = "staffMember-profile-test-request";
        const string privateExceptionMessage = "private database detail";
        var context = new DefaultHttpContext
        {
            TraceIdentifier = requestId
        };
        context.Response.Body = new MemoryStream();
        var middleware = new ErrorHandlerMiddleware(
            _ => throw new InvalidOperationException(privateExceptionMessage),
            NullLogger<ErrorHandlerMiddleware>.Instance);

        await middleware.InvokeAsync(context);
        context.Response.Body.Position = 0;
        using var response = await JsonDocument.ParseAsync(context.Response.Body);
        var root = response.RootElement;

        Assert.Multiple(() =>
        {
            Assert.That(context.Response.StatusCode, Is.EqualTo(StatusCodes.Status500InternalServerError));
            Assert.That(root.GetProperty("Message").GetString(), Does.Contain("contact support"));
            Assert.That(root.GetProperty("Errors")[0].GetString(), Is.EqualTo($"Request ID: {requestId}"));
            Assert.That(root.ToString(), Does.Not.Contain(privateExceptionMessage));
        });
    }

    [TestCase(0, 403)]
    [TestCase(-1, 401)]
    [TestCase(-2, 401)]
    public async Task Institution_permission_result_preserves_permission_vs_authentication_status(int result, int status)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new ErrorHandlerMiddleware(_ =>
        {
            InstitutionRuntimePermissionValidator.EnsureAllowed(new InstitutionsUserPermissionCheckResponseDTO { ResultCode = result });
            return Task.CompletedTask;
        }, NullLogger<ErrorHandlerMiddleware>.Instance);
        await middleware.InvokeAsync(context);
        Assert.That(context.Response.StatusCode, Is.EqualTo(status));
    }

    [TestCase(0, 4)]
    [TestCase(78, 0)]
    [TestCase(78, -1)]
    public async Task Missing_action_identifiers_do_not_become_authentication_401(int moduleId, int operationId)
    {
        // No provider is configured: any accidental query fails before a connection is possible.
        await using var db = new SkolenzaDbContext(new DbContextOptionsBuilder<SkolenzaDbContext>().Options);
        var mapper = new MapperConfiguration(_ => { }).CreateMapper();
        var repository = new StoreProcedureRepository(db, NullLogger<StoreProcedureRepository>.Instance, mapper);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new ErrorHandlerMiddleware(async _ =>
        {
            await repository.CheckInstitutionStaffMemberPermissionAsync(8, 50, 7, moduleId, operationId);
        }, NullLogger<ErrorHandlerMiddleware>.Instance);
        await middleware.InvokeAsync(context);
        Assert.That(context.Response.StatusCode, Is.EqualTo(400));
    }
}
