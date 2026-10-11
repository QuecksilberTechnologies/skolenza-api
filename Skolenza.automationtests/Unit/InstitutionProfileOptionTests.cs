using skolenza.application.Constants;
using skolenza.application.DTOs.Institution;
using skolenza.application.DTOs.InstitutionType;
using skolenza.api.Controllers.InstitutionType;
using skolenza.domain.Entity;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("InstitutionProfileOptions")]
public sealed class InstitutionProfileOptionTests
{
    [Test]
    public void Institution_type_contract_has_no_institution_industry_names()
    {
        var controllerRoutes = typeof(InstitutionTypeController)
            .GetCustomAttributes(typeof(RouteAttribute), inherit: true)
            .Cast<RouteAttribute>()
            .Select(attribute => attribute.Template)
            .ToArray();

        var actionRoute = typeof(InstitutionTypeController)
            .GetMethod(nameof(InstitutionTypeController.GetAllInstitutionBySubscriptionIdAsync))!
            .GetCustomAttributes(typeof(HttpGetAttribute), inherit: true)
            .Cast<HttpGetAttribute>()
            .Single()
            .Template;

        Assert.Multiple(() =>
        {
            Assert.That(controllerRoutes, Is.EqualTo(new[] { "api/InstitutionType" }));
            Assert.That(actionRoute, Is.EqualTo("get-institution-types"));
            Assert.That(typeof(InstitutionType).GetProperty("InstitutionTypeId"), Is.Null);
            Assert.That(typeof(InstitutionType).GetProperty(nameof(InstitutionType.InstitutionTypeName)), Is.Not.Null);
            Assert.That(typeof(InstitutionTypeResponseDTO).GetProperty(nameof(InstitutionTypeResponseDTO.InstitutionTypeName)), Is.Not.Null);
        });
    }

    [Test]
    public void Institution_categories_are_unique_authoritative_string_values()
    {
        var options = AppConstants.InstitutionCategories;

        Assert.Multiple(() =>
        {
            Assert.That(options.Select(option => option.Id), Is.Unique);
            Assert.That(options.Select(option => option.Code), Is.Unique);
            Assert.That(options.Select(option => option.Name), Is.Unique);
            Assert.That(options.Select(option => option.Name), Does.Contain("Private"));
            Assert.That(AppConstants.IsSupportedInstitutionCategory("Private"), Is.True);
            Assert.That(AppConstants.IsSupportedInstitutionCategory("Manufacturing"), Is.False);
            Assert.That(typeof(InstitutionProfile).GetProperty(nameof(InstitutionProfile.BusinessType))!.PropertyType,
                Is.EqualTo(typeof(string)));
            Assert.That(typeof(NewInstitutionProfileRequestDTO).GetProperty(nameof(NewInstitutionProfileRequestDTO.BusinessType))!.PropertyType,
                Is.EqualTo(typeof(string)));
        });
    }

    [Test]
    public void Campus_types_reuse_stable_smallint_location_values()
    {
        var options = AppConstants.InstitutionCampusTypes;

        Assert.Multiple(() =>
        {
            Assert.That(options.Select(option => option.Id), Is.Unique);
            Assert.That(options.Select(option => option.Code), Is.Unique);
            Assert.That(options.Select(option => option.Code), Does.Contain("MAIN_CAMPUS"));
            Assert.That(options.Select(option => option.Code), Does.Not.Contain("PLANT"));
            Assert.That(options.All(option => option.Id >= short.MinValue && option.Id <= short.MaxValue), Is.True);
            Assert.That(options.All(option => AppConstants.IsSupportedInstitutionCampusType((short)option.Id)), Is.True);
            Assert.That(typeof(Campus).GetProperty(nameof(Campus.LocationType))!.PropertyType,
                Is.EqualTo(typeof(short)));
            Assert.That(typeof(NewCampusRequestDTO).GetProperty(nameof(NewCampusRequestDTO.LocationType))!.PropertyType,
                Is.EqualTo(typeof(short)));
        });
    }
}
