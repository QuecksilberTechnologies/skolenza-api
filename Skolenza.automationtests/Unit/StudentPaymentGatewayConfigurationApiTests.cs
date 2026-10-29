using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using NUnit.Framework;
using skolenza.api.Controllers.Student;
using skolenza.application.Common.Enums;
using skolenza.application.Constants;
using skolenza.application.DTOS.Student;
using skolenza.application.Features.StudentCmd;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("StudentCashfreeConfiguration")]
public sealed class StudentPaymentGatewayConfigurationApiTests
{
    [Test]
    public void Cashfree_environment_is_exposed_from_the_central_student_options_contract()
    {
        Assert.That(Enum.GetValues<StudentPaymentGatewayEnvironment>(), Is.EqualTo(new[]
        {
            StudentPaymentGatewayEnvironment.Sandbox,
            StudentPaymentGatewayEnvironment.Production
        }));
        Assert.That(typeof(StudentOptionsResponseDTO).GetProperty("PaymentGatewayEnvironments"), Is.Not.Null);
    }

    [Test]
    public void Gateway_configuration_routes_are_authenticated_student_fee_api_routes()
    {
        var controller = typeof(StudentFeeLedgerController);
        var route = controller.GetCustomAttribute<RouteAttribute>()?.Template;
        var authorization = controller.GetCustomAttribute<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>();
        var actions = controller.GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .SelectMany(method => method.GetCustomAttributes<HttpMethodAttribute>()
                .Where(attribute => attribute.Template == "gateway-configuration")
                .Select(attribute => new { method.Name, Method = attribute.HttpMethods.Single() }))
            .ToList();

        Assert.Multiple(() =>
        {
            Assert.That(route, Is.EqualTo("api/student/fees"));
            Assert.That(authorization, Is.Not.Null);
            Assert.That(actions.Select(item => item.Method), Is.EquivalentTo(new[] { "GET", "POST", "PUT" }));
        });
    }

    [Test]
    public void Gateway_configuration_response_does_not_expose_secret_values()
    {
        var propertyNames = typeof(StudentPaymentGatewayConfigurationResponseDTO)
            .GetProperties().Select(property => property.Name).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(propertyNames, Does.Not.Contain("ClientSecret"));
            Assert.That(propertyNames, Does.Not.Contain("WebhookSecret"));
            Assert.That(propertyNames, Does.Not.Contain("ProtectedClientSecret"));
            Assert.That(propertyNames, Does.Not.Contain("ProtectedWebhookSecret"));
            Assert.That(StudentConstants.CashfreeGatewayCode, Is.EqualTo("CASHFREE"));
        });
    }
}
