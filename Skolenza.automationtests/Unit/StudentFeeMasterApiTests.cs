using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using NUnit.Framework;
using skolenza.api.Controllers.Student;
using skolenza.application.Common.Enums;
using skolenza.application.Constants;
using skolenza.application.DTOS.Student;
using skolenza.application.Features.StudentCmd;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("StudentFeeMaster")]
public sealed class StudentFeeMasterApiTests
{
    [Test]
    public void Fee_master_entities_map_to_separate_institution_master_tables()
    {
        using var context = new SkolenzaDbContext(
            new DbContextOptionsBuilder<SkolenzaDbContext>()
                .UseNpgsql("Host=localhost;Database=skolenza_model_validation;Username=postgres;Password=not-used")
                .Options);

        var frequency = context.Model.FindEntityType(typeof(StudentFeeFrequencyMaster));
        var component = context.Model.FindEntityType(typeof(StudentFeeComponentMaster));

        Assert.Multiple(() =>
        {
            Assert.That(frequency?.GetTableName(), Is.EqualTo("StudentFeeFrequencyMaster"));
            Assert.That(frequency?.GetSchema(), Is.EqualTo("skolenza"));
            Assert.That(component?.GetTableName(), Is.EqualTo("StudentFeeComponentMaster"));
            Assert.That(component?.GetSchema(), Is.EqualTo("skolenza"));
            Assert.That(frequency?.FindProperty(nameof(StudentFeeFrequencyMaster.InstallmentsPerYear)), Is.Not.Null);
            Assert.That(component?.FindProperty(nameof(StudentFeeComponentMaster.ChargeNature)), Is.Not.Null);
        });
    }

    [Test]
    public void Student_owned_master_identifiers_use_64_bit_types()
    {
        var studentMasterTypes = new[]
        {
            typeof(StudentDocumentRequirementMaster),
            typeof(StudentFeeFrequencyMaster),
            typeof(StudentFeeComponentMaster),
            typeof(StudentBenefitSchemeMaster)
        };

        foreach (var entityType in studentMasterTypes)
        {
            var idProperty = entityType.GetProperty(nameof(StudentInstitutionMasterEntity.Id));
            var institutionIdProperty = entityType.GetProperty(nameof(StudentInstitutionMasterEntity.InstitutionId));
            var templateSourceIdProperty = entityType.GetProperty(nameof(StudentInstitutionMasterEntity.TemplateSourceId));

            Assert.That(idProperty?.PropertyType, Is.EqualTo(typeof(long)), $"{entityType.Name}.Id");
            Assert.That(institutionIdProperty?.PropertyType, Is.EqualTo(typeof(long?)), $"{entityType.Name}.InstitutionId");
            Assert.That(templateSourceIdProperty?.PropertyType, Is.EqualTo(typeof(long?)), $"{entityType.Name}.TemplateSourceId");
        }

        Assert.Multiple(() =>
        {
            Assert.That(typeof(StudentBenefitSchemeMaster).GetProperty(nameof(StudentBenefitSchemeMaster.EvidenceRequirementId))?.PropertyType,
                Is.EqualTo(typeof(long?)));
            Assert.That(typeof(StudentBenefitSchemeMaster).GetProperty(nameof(StudentBenefitSchemeMaster.CountryId))?.PropertyType,
                Is.EqualTo(typeof(int?)));
            Assert.That(typeof(StudentBenefitSchemeMaster).GetProperty(nameof(StudentBenefitSchemeMaster.StateId))?.PropertyType,
                Is.EqualTo(typeof(int?)));
        });
    }

    [Test]
    public void Student_master_bigint_migration_preserves_values_and_widens_identity_sequences()
    {
        var root = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", ".."));
        var script = File.ReadAllText(Path.Combine(root, "database-scripts", "MigrateStudentMasterIdentifiersToBigint.sql"));

        Assert.Multiple(() =>
        {
            Assert.That(script, Does.Contain("ALTER COLUMN \"Id\" TYPE bigint USING \"Id\"::bigint"));
            Assert.That(script, Does.Contain("ALTER COLUMN \"TemplateSourceId\" TYPE bigint USING \"TemplateSourceId\"::bigint"));
            Assert.That(script, Does.Contain("ALTER COLUMN \"EvidenceRequirementId\" TYPE bigint USING \"EvidenceRequirementId\"::bigint"));
            Assert.That(script, Does.Contain("pg_get_serial_sequence"));
            Assert.That(script, Does.Contain("ALTER SEQUENCE %s AS bigint"));
            Assert.That(script, Does.Contain("CountryId and StateId retain their source catalogue integer types."));
        });
    }

    [Test]
    public void Admission_status_values_are_centralized_and_match_the_approved_workflow()
    {
        Assert.Multiple(() =>
        {
            Assert.That((short)StudentAdmissionStatus.Draft, Is.EqualTo(1));
            Assert.That((short)StudentAdmissionStatus.Submitted, Is.EqualTo(2));
            Assert.That((short)StudentAdmissionStatus.UnderReview, Is.EqualTo(3));
            Assert.That((short)StudentAdmissionStatus.Approved, Is.EqualTo(4));
            Assert.That((short)StudentAdmissionStatus.Rejected, Is.EqualTo(5));
            Assert.That((short)StudentAdmissionStatus.ReturnedForCorrection, Is.EqualTo(6));
            Assert.That((short)StudentAdmissionStatus.Cancelled, Is.EqualTo(7));
        });
    }

    [Test]
    public void Fee_master_controllers_are_authenticated_and_expose_expected_routes()
    {
        AssertController(typeof(StudentFeeFrequencyController),
            "api/Student/fee-frequency-masters", "GetPage", "Create", "Update", "Delete");
        AssertController(typeof(StudentFeeComponentController),
            "api/Student/fee-component-masters", "GetPage", "Create", "Update", "Delete");
        AssertLongIdRoutes(typeof(StudentFeeFrequencyController));
        AssertLongIdRoutes(typeof(StudentFeeComponentController));
        AssertLongIdRoutes(typeof(StudentBenefitSchemeController));
        AssertLongIdRoutes(typeof(StudentDocumentRequirementController));
    }

    [Test]
    public void Student_create_commands_use_the_existing_Add_permission_operation()
    {
        var commands = new object[]
        {
            new CreateStudentDocumentRequirementCommand(new StudentDocumentRequirementRequestDTO()),
            new CreateStudentFeeFrequencyCommand(new StudentFeeFrequencyRequestDTO()),
            new CreateStudentFeeComponentCommand(new StudentFeeComponentRequestDTO()),
            new CreateStudentBenefitSchemeCommand(new StudentBenefitSchemeRequestDTO())
        };
        var resolver = typeof(StudentPermissionBehavior<object, object>)
            .GetMethod("ResolvePermission", BindingFlags.NonPublic | BindingFlags.Static);

        Assert.That(resolver, Is.Not.Null);
        foreach (var command in commands)
        {
            var permission = resolver!.Invoke(null, [command]);
            var operationName = permission?.GetType().GetField("Item2")?.GetValue(permission);
            Assert.That(operationName, Is.EqualTo("Add"), command.GetType().Name);
        }
    }

    [Test]
    public void Fee_master_filters_use_the_central_paging_defaults()
    {
        var frequency = new StudentFeeFrequencyFilterDTO();
        var component = new StudentFeeComponentFilterDTO();

        Assert.Multiple(() =>
        {
            Assert.That(frequency.PageNumber, Is.EqualTo(1));
            Assert.That(frequency.PageSize, Is.EqualTo(10));
            Assert.That(component.PageNumber, Is.EqualTo(1));
            Assert.That(component.PageSize, Is.EqualTo(10));
            Assert.That(frequency.ActiveOnly, Is.True);
            Assert.That(component.ActiveOnly, Is.True);
        });
    }

    [Test]
    public void Component_charge_nature_distinguishes_annual_and_one_time_charges()
    {
        Assert.Multiple(() =>
        {
            Assert.That((short)StudentFeeComponentChargeNature.RecurringAnnual, Is.EqualTo(1));
            Assert.That((short)StudentFeeComponentChargeNature.OneTime, Is.EqualTo(2));
            Assert.That(Enum.IsDefined((StudentFeeComponentChargeNature)3), Is.False);
        });
    }

    [Test]
    public void Fee_master_migration_adds_dynamic_fee_module_operations_without_fixed_ids()
    {
        var root = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", ".."));
        var script = File.ReadAllText(Path.Combine(root, "database-scripts", "CreateStudentFeeMastersAndModule.sql"));

        Assert.Multiple(() =>
        {
            Assert.That(script, Does.Contain(StudentConstants.FeesModuleCode));
            Assert.That(script, Does.Contain("CREATE TABLE IF NOT EXISTS skolenza.\"StudentFeeFrequencyMaster\""));
            Assert.That(script, Does.Contain("CREATE TABLE IF NOT EXISTS skolenza.\"StudentFeeComponentMaster\""));
            Assert.That(script, Does.Contain("CROSS JOIN skolenza.\"Operation\""));
            Assert.That(script, Does.Contain("'view', 'add', 'update', 'delete'"));
            Assert.That(script, Does.Contain("InstallmentsPerYear"));
            Assert.That(script, Does.Contain("ChargeNature"));
        });
    }

    [Test]
    public void Benefit_scheme_controller_and_filter_follow_the_authenticated_paged_master_contract()
    {
        var controller = typeof(StudentBenefitSchemeController);
        var filter = new StudentBenefitSchemeFilterDTO();

        Assert.Multiple(() =>
        {
            Assert.That(controller.GetCustomAttributes(typeof(AuthorizeAttribute), true), Is.Not.Empty);
            Assert.That(controller.GetCustomAttributes(typeof(RouteAttribute), true)
                .Cast<RouteAttribute>().Single().Template, Is.EqualTo("api/Student/benefit-scheme-masters"));
            Assert.That(controller.GetMethods().Select(method => method.Name),
                Does.Contain("GetPage").And.Contain("Create").And.Contain("Update").And.Contain("Delete"));
            Assert.That(filter.PageNumber, Is.EqualTo(1));
            Assert.That(filter.PageSize, Is.EqualTo(10));
            Assert.That(filter.ActiveOnly, Is.True);
        });
    }

    [Test]
    public void Benefit_scheme_persistence_maps_geographic_and_discount_scope()
    {
        using var context = new SkolenzaDbContext(
            new DbContextOptionsBuilder<SkolenzaDbContext>()
                .UseNpgsql("Host=localhost;Database=skolenza_model_validation;Username=postgres;Password=not-used")
                .Options);

        var entity = context.Model.FindEntityType(typeof(StudentBenefitSchemeMaster));

        Assert.Multiple(() =>
        {
            Assert.That(entity?.GetTableName(), Is.EqualTo("StudentBenefitSchemeMaster"));
            Assert.That(entity?.GetSchema(), Is.EqualTo("skolenza"));
            Assert.That(entity?.GetIndexes().Any(index => index.IsUnique), Is.True);
            Assert.That(entity?.FindProperty(nameof(StudentBenefitSchemeMaster.CountryId)), Is.Not.Null);
            Assert.That(entity?.FindProperty(nameof(StudentBenefitSchemeMaster.StateId)), Is.Not.Null);
            Assert.That(entity?.FindProperty(nameof(StudentBenefitSchemeMaster.CampusId)), Is.Not.Null);
            Assert.That(entity?.FindProperty(nameof(StudentBenefitSchemeMaster.AcademicSessionId)), Is.Not.Null);
            Assert.That(entity?.FindProperty(nameof(StudentBenefitSchemeMaster.MinimumDiscountPercent))?.GetPrecision(), Is.EqualTo(5));
            Assert.That(entity?.FindProperty(nameof(StudentBenefitSchemeMaster.MaximumDiscountPercent))?.GetScale(), Is.EqualTo(2));
        });
    }

    [Test]
    public void Benefit_scheme_migration_reuses_geography_and_dynamic_admissions_permissions()
    {
        var root = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", ".."));
        var script = File.ReadAllText(Path.Combine(root, "database-scripts", "CreateStudentBenefitSchemeMaster.sql"));

        Assert.Multiple(() =>
        {
            Assert.That(script, Does.Contain(StudentConstants.AdmissionsModuleCode));
            Assert.That(script, Does.Contain("REFERENCES skolenza.\"Country\""));
            Assert.That(script, Does.Contain("REFERENCES skolenza.\"State\""));
            Assert.That(script, Does.Contain("REFERENCES skolenza.\"Campus\""));
            Assert.That(script, Does.Contain("MinimumDiscountPercent"));
            Assert.That(script, Does.Contain("MaximumDiscountPercent"));
            Assert.That(script, Does.Contain("CROSS JOIN skolenza.\"Operation\""));
            Assert.That(script, Does.Contain("'view', 'add', 'update', 'delete'"));
        });
    }

    private static void AssertController(Type controller, string route, params string[] actions)
    {
        var routeAttribute = controller.GetCustomAttributes(typeof(RouteAttribute), true)
            .Cast<RouteAttribute>().SingleOrDefault();
        var actionNames = controller.GetMethods().Select(method => method.Name).ToHashSet();

        Assert.Multiple(() =>
        {
            Assert.That(controller.GetCustomAttributes(typeof(AuthorizeAttribute), true), Is.Not.Empty);
            Assert.That(routeAttribute?.Template, Is.EqualTo(route));
            foreach (var action in actions)
            {
                Assert.That(actionNames, Does.Contain(action));
            }
        });
    }

    private static void AssertLongIdRoutes(Type controller)
    {
        var idRoutes = controller.GetMethods()
            .SelectMany(method => method.GetCustomAttributes(typeof(HttpMethodAttribute), true)
                .Cast<HttpMethodAttribute>())
            .Where(attribute => attribute.Template?.Contains("{id", StringComparison.Ordinal) == true)
            .Select(attribute => attribute.Template)
            .ToArray();

        Assert.That(idRoutes, Is.Not.Empty, $"{controller.Name} must expose ID-based update/delete routes.");
        Assert.That(idRoutes, Is.All.Contains("{id:long}"),
            $"{controller.Name} route constraints must support PostgreSQL bigint IDs.");
    }
}
