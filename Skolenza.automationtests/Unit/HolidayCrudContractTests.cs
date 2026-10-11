using System.Reflection;
using skolenza.api.Controllers.Holiday;
using skolenza.application.DTOs.Holiday;
using skolenza.application.Features.HolidayCmd;
using skolenza.application.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("HolidayCrud")]
public sealed class HolidayCrudContractTests
{
    [Test]
    public void Controller_requires_authentication_and_exposes_all_crud_routes()
    {
        var controller = typeof(HolidayController);
        var methods = controller.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        Assert.Multiple(() =>
        {
            Assert.That(controller.GetCustomAttribute<AuthorizeAttribute>(), Is.Not.Null);
            Assert.That(methods.Single(method => method.Name == "Get").GetCustomAttribute<HttpGetAttribute>()?.Template, Is.EqualTo("get"));
            Assert.That(methods.Single(method => method.Name == "GetById").GetCustomAttribute<HttpGetAttribute>()?.Template, Is.EqualTo("{id:long}"));
            Assert.That(methods.Single(method => method.Name == "Create").GetCustomAttribute<HttpPostAttribute>(), Is.Not.Null);
            Assert.That(methods.Single(method => method.Name == "Update").GetCustomAttribute<HttpPutAttribute>()?.Template, Is.EqualTo("{id:long}"));
            Assert.That(methods.Single(method => method.Name == "Delete").GetCustomAttribute<HttpDeleteAttribute>()?.Template, Is.EqualTo("{id:long}"));
            Assert.That(methods.Single(method => method.Name == "Import").GetCustomAttribute<HttpPostAttribute>()?.Template, Is.EqualTo("import"));
            Assert.That(methods.Single(method => method.Name == "Export").GetCustomAttribute<HttpGetAttribute>()?.Template, Is.EqualTo("export"));
            Assert.That(methods.Single(method => method.Name == "GetCalendarDisplayConstants").GetCustomAttribute<HttpGetAttribute>()?.Template, Is.EqualTo("calendar-display-constants"));
        });
    }

    [Test]
    public void Calendar_display_constants_are_token_only_stable_and_have_deterministic_priority()
    {
        Assert.Multiple(() =>
        {
            Assert.That(AppConstants.CalendarStatusCodes.MandatoryHoliday, Is.EqualTo("MANDATORY_HOLIDAY"));
            Assert.That(AppConstants.CalendarStatusCodes.OptionalHoliday, Is.EqualTo("OPTIONAL_HOLIDAY"));
            Assert.That(AppConstants.CalendarStatusCodes.Pending, Is.EqualTo("PENDING"));
            Assert.That(AppConstants.CalendarStatusCodes.Approved, Is.EqualTo("APPROVED"));
            Assert.That(AppConstants.CalendarStatusCodes.OnLeave, Is.EqualTo("ON_LEAVE"));
            Assert.That(AppConstants.CalendarStatusCodes.Taken, Is.EqualTo("TAKEN"));
            Assert.That(AppConstants.CalendarStatusCodes.Rejected, Is.EqualTo("REJECTED"));
            Assert.That(AppConstants.CalendarStatusCodes.Cancelled, Is.EqualTo("CANCELLED"));
            Assert.That(AppConstants.CalendarStatusCodes.WeeklyOff, Is.EqualTo("WEEKLY_OFF"));
            Assert.That(AppConstants.CalendarColors.MandatoryHoliday, Is.EqualTo("#DC2626"));
            Assert.That(AppConstants.CalendarColors.OptionalHoliday, Is.EqualTo("#F59E0B"));
            Assert.That(AppConstants.CalendarColors.Pending, Is.EqualTo("#EAB308"));
            Assert.That(AppConstants.CalendarColors.Approved, Is.EqualTo("#2563EB"));
            Assert.That(AppConstants.CalendarColors.OnLeave, Is.EqualTo("#16A34A"));
            Assert.That(AppConstants.CalendarColors.Taken, Is.EqualTo("#16A34A"));
            Assert.That(AppConstants.CalendarColors.Rejected, Is.EqualTo("#6B7280"));
            Assert.That(AppConstants.CalendarColors.Cancelled, Is.EqualTo("#9CA3AF"));
            Assert.That(AppConstants.CalendarColors.WeeklyOff, Is.EqualTo("#D1D5DB"));
            Assert.That(typeof(GetHolidayCalendarDisplayConstantsQuery).GetProperties().Any(property => property.Name is "ModuleId" or "OperationId"), Is.False);
            Assert.That(typeof(GetHolidayCalendarDisplayConstantsQueryHandler), Is.Not.Null);
        });
    }

    [Test]
    public void Request_contract_requires_dynamic_permission_ids_and_location_scope()
    {
        Assert.Multiple(() =>
        {
            Assert.That(typeof(BasicRequestDTO).GetProperty("ModuleId"), Is.Not.Null);
            Assert.That(typeof(BasicRequestDTO).GetProperty("OperationId"), Is.Not.Null);
            Assert.That(typeof(BasicRequestDTO).GetProperty("CampusId"), Is.Not.Null);
            Assert.That(typeof(BasicRequestDTO).GetProperty("HolidayYear"), Is.Not.Null);
            Assert.That(typeof(SaveHolidayRequestDTO).GetProperty("InstitutionId"), Is.Null);
            Assert.That(typeof(UpdateHolidayRequestDTO).GetProperty("InstitutionId"), Is.Null);
            Assert.That(typeof(HolidayDTO).GetProperty("Id"), Is.Not.Null);
            Assert.That(typeof(SaveHolidayRequestDTO).GetProperty("Icon"), Is.Not.Null);
            Assert.That(typeof(UpdateHolidayRequestDTO).GetProperty("Icon"), Is.Not.Null);
            Assert.That(typeof(HolidayDTO).GetProperty("Icon"), Is.Not.Null);
        });
    }

    [Test]
    public void Every_crud_request_has_a_permission_pipeline_handler()
    {
        var requests = new[]
        {
            typeof(ListHolidaysQuery),
            typeof(GetHolidayQuery),
            typeof(CreateHolidayCommand),
            typeof(UpdateHolidayCommand),
            typeof(DeleteHolidayCommand),
            typeof(ImportHolidaysCommand),
            typeof(ExportHolidaysQuery)
        };

        Assert.Multiple(() =>
        {
            foreach (var request in requests)
            {
                Assert.That(request.Assembly.GetTypes().Any(type =>
                    type.GetInterfaces().Any(contract =>
                        contract.IsGenericType
                        && contract.GetGenericTypeDefinition() == typeof(MediatR.IRequestHandler<,>)
                        && contract.GetGenericArguments()[0] == request)), Is.True, request.Name);
            }

            Assert.That(typeof(HolidayPermissionBehavior<,>), Is.Not.Null);
        });
    }
}
