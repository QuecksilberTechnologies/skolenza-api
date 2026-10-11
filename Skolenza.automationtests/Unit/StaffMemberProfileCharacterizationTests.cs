// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Locks the current StaffMember profile percentage and API
//           contracts before any common-policy refactoring.
// ================================================================

using System.Reflection;
using AutoMapper;
using skolenza.api.Controllers.Staff;
using skolenza.application.Common.Enums;
using skolenza.application.Common.Helpers;
using skolenza.application.Common.Helpers.ProjectionHelpers.Staff;
using skolenza.application.Common.Helpers.PercentageHelper;
using skolenza.application.DTOS.Staff.Bank;
using skolenza.application.DTOS.Staff.CompletionPercentage;
using skolenza.application.DTOS.Staff.Contact;
using skolenza.application.DTOS.Staff.Education;
using skolenza.application.DTOS.Staff.Dependent;
using skolenza.application.DTOS.Staff.Experience;
using skolenza.application.DTOs.BaseDTO;
using skolenza.application.DTOS.StoreProcedures;
using skolenza.application.Extentions;
using skolenza.application.Features.StaffMemberCmd;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Mappings;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using NUnit.Framework;

namespace skolenza.automationtests.Unit;

/// <summary>
/// Characterizes current StaffMember profile behavior. These assertions intentionally
/// describe the existing output and must be reviewed before changing a formula or contract.
/// </summary>
[TestFixture]
[Category("StaffMemberProfileCharacterization")]
public sealed class StaffMemberProfileCharacterizationTests
{
    #region Percentage behavior

    /// <summary>
    /// Verifies an absent section remains a non-created, zero-percent, locked section.
    /// </summary>
    [Test]
    public void Empty_supported_sections_keep_the_current_zero_percent_contract()
    {
        var education = Array.Empty<GetEducationResponseDTO>().CalculateEducationCompletionDTO();
        var bank = Array.Empty<GetBankResponseDTO>().CalculateBankCompletionDTO();
        var contact = Array.Empty<GetContactResponseDTO>().CalculateContactCompletionDTO();

        Assert.Multiple(() =>
        {
            AssertEmpty(education, "Education");
            AssertEmpty(bank, "Bank");
            AssertEmpty(contact, "Contact");
        });
    }

    /// <summary>
    /// Locks the current six-field Education formula and first-row status behavior.
    /// </summary>
    [Test]
    public void Education_uses_six_equal_checks_and_first_row_status()
    {
        var result = new[]
        {
            new GetEducationResponseDTO
            {
                Degree = "BSc",
                InstituteName = "Example University",
                StartDate = new DateOnly(2020, 1, 1),
                EndDate = new DateOnly(2023, 1, 1),
                HasEducationDocUploded = true,
                ScoreType = "Percentage",
                IsInfoVerified = true,
                IsEditAllowed = false
            }
        }.CalculateEducationCompletionDTO();

        Assert.Multiple(() =>
        {
            Assert.That(result.CompletionPercent, Is.EqualTo(100));
            Assert.That(result.IsInfoVerified, Is.True);
            Assert.That(result.IsEditAllowed, Is.False);
            Assert.That(result.IsSectionCreate, Is.True);
        });
    }

    [Test]
    public void Education_create_mapping_accepts_the_form_contract()
    {
        var mapper = new MapperConfiguration(configuration =>
            configuration.AddProfile<MappingProfile>()).CreateMapper();
        var request = new CreateEducationRequestDTO
        {
            Degree = "Bachelor of Technology in Computer Science",
            InstituteName = "Example Institute",
            StartDate = new DateTime(2016, 7, 1),
            EndDate = new DateTime(2020, 6, 30),
            ScoreType = "1",
            ScoreValue = "80",
            GradeDivision = "First"
        };

        var result = mapper.Map<StaffMemberEducation>(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.StartDate, Is.EqualTo(new DateOnly(2016, 7, 1)));
            Assert.That(result.EndDate, Is.EqualTo(new DateOnly(2020, 6, 30)));
            Assert.That(result.ScoreType, Is.EqualTo(1));
        });
    }

    /// <summary>
    /// Locks the primary Bank requirement: a section without a primary account is incomplete.
    /// </summary>
    [Test]
    public void Bank_without_a_primary_account_is_zero_percent()
    {
        var result = new[]
        {
            new GetBankResponseDTO
            {
                BankName = "Example Bank",
                BranchName = "Main",
                IFSCCode = "TEST0001",
                AccountNumber = "1234",
                AccountType = "Savings",
                IsPrimaryAccount = false,
                IsInfoVerified = true,
                IsEditAllowed = true
            }
        }.CalculateBankCompletionDTO();

        Assert.Multiple(() =>
        {
            Assert.That(result.CompletionPercent, Is.EqualTo(0));
            Assert.That(result.IsInfoVerified, Is.True);
            Assert.That(result.IsEditAllowed, Is.False,
                "The approved verification lock overrides stale stored edit flags without changing completion.");
        });
    }

    [Test]
    public void Bank_with_multiple_rows_uses_the_average_after_primary_validation()
    {
        var result = new[]
        {
            new GetBankResponseDTO
            {
                BankName = "Primary Bank",
                BranchName = "Main",
                IFSCCode = "TEST0001",
                AccountNumber = "1234",
                AccountType = "Savings",
                IsPrimaryAccount = true,
                HasChequeDocUploaded = true,
                FileName = "cheque.pdf",
                FilePath = "staffMembers/cheque.pdf"
            },
            new GetBankResponseDTO
            {
                BankName = "Secondary Bank",
                AccountNumber = "5678",
                AccountType = "Savings",
                IsPrimaryAccount = false
            }
        }.CalculateBankCompletionDTO();

        Assert.That(result.CompletionPercent, Is.EqualTo(84));
    }

    [TestCase(null, false, true, false, 0)]
    [TestCase("PAN123", false, true, true, 50)]
    [TestCase("PAN123", true, true, true, 100)]
    [TestCase("PASS123", false, false, true, 50)]
    [TestCase(null, false, false, false, 0)]
    public void Identity_completion_uses_country_mandatory_and_uploaded_document_rules(
        string? identityValue,
        bool hasIdentityUploaded,
        bool isMandatory,
        bool hasSavedRecord,
        double expected)
    {
        var result = StaffMemberProfileCompletionCalculator.CalculateIdentityRow(
            identityValue,
            hasIdentityUploaded,
            isMandatory,
            hasSavedRecord);

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void Overall_completion_is_the_same_average_exposed_to_list_and_profile_status()
    {
        var result = StaffMemberProfileCompletionCalculator.CalculateOverallCompletion(
            new[]
            {
                new CompletionSectionDTO { CompletionPercent = 100 },
                new CompletionSectionDTO { CompletionPercent = 50 },
                new CompletionSectionDTO { CompletionPercent = 0 }
            });

        Assert.That(result, Is.EqualTo(50));
    }

    /// <summary>
    /// Locks the current multi-row status aggregation: all verified and any editable.
    /// </summary>
    [Test]
    public void Contact_aggregates_all_verified_and_any_editable_across_rows()
    {
        var result = new[]
        {
            new GetContactResponseDTO
            {
                Id = 1,
                ContactName = "One",
                ContactNumber = "100",
                Relation = 1,
                IsPrimary = true,
                IsInfoVerified = true,
                IsEditAllowed = false
            },
            new GetContactResponseDTO
            {
                Id = 2,
                ContactName = "Two",
                ContactNumber = "200",
                Relation = 2,
                IsPrimary = false,
                IsInfoVerified = false,
                IsEditAllowed = true
            }
        }.CalculateContactCompletionDTO();

        Assert.Multiple(() =>
        {
            Assert.That(result.CompletionPercent, Is.EqualTo(100));
            Assert.That(result.IsInfoVerified, Is.False);
            Assert.That(result.IsEditAllowed, Is.True);
        });
    }

    /// <summary>
    /// Verifies the common calculator excludes workflow flags from data completion.
    /// </summary>
    [Test]
    public void Common_calculator_uses_only_required_data_fields()
    {
        var percentage = StaffMemberProfileCompletionCalculator.CalculateRowPercentage(
            true,
            false,
            true,
            true);

        Assert.That(percentage, Is.EqualTo(75));
    }

    /// <summary>
    /// Verifies verification always overrides editability in the common section result.
    /// </summary>
    [Test]
    public void Verified_section_is_never_reported_as_editable()
    {
        var result = StaffMemberProfileCompletionCalculator.CreateSection(
            "Identity",
            new[] { 100d },
            new bool?[] { true },
            new bool?[] { true });

        Assert.Multiple(() =>
        {
            Assert.That(result.IsInfoVerified, Is.True);
            Assert.That(result.IsEditAllowed, Is.False);
        });
    }

    [Test]
    public void Legacy_row_helpers_delegate_to_the_central_profile_calculator()
    {
        var experience = new GetStaffMemberExperienceResponseDTO
        {
            CompanyName = "Example Ltd",
            Designation = "Engineer",
            StartDate = DateTime.UtcNow.AddYears(-2),
            EndDate = DateTime.UtcNow.AddYears(-1),
            IsInfoVerified = false,
            IsEditAllowed = true
        };
        var dependent = new GetDependentResponseDTO
        {
            Id = 1,
            DependentName = "Example Dependent",
            Relation = 1,
            DateOfBirth = DateTime.UtcNow.AddYears(-20),
            HasProofUploaded = true,
            IsInfoVerified = false,
            IsEditAllowed = true
        };

        Assert.Multiple(() =>
        {
            Assert.That(
                CompletionCalculatorHelper.ExperiencePropCalculate(experience),
                Is.EqualTo(StaffMemberProfileCompletionCalculator.CalculateExperienceRow(experience)));
            Assert.That(
                CompletionCalculatorHelper.DependentPropCalculate(dependent),
                Is.EqualTo(StaffMemberProfileCompletionCalculator.CalculateDependentRow(dependent)));
        });
    }

    [Test]
    public void Workflow_flags_do_not_change_row_completion_percentage()
    {
        var bank = new GetBankResponseDTO
        {
            BankName = "Example Bank",
            BranchName = "Main",
            IFSCCode = "TEST0001",
            AccountNumber = "123456789",
            AccountType = "Savings",
            IsPrimaryAccount = false,
            IsInfoVerified = false,
            IsEditAllowed = true
        };

        var before = StaffMemberProfileCompletionCalculator.CalculateBankRow(bank);
        bank.IsInfoVerified = true;
        bank.IsEditAllowed = false;
        var after = StaffMemberProfileCompletionCalculator.CalculateBankRow(bank);

        Assert.That(after, Is.EqualTo(before));
    }

    [Test]
    public void Shared_profile_validation_rejects_reversed_date_ranges()
    {
        Assert.Throws<skolenza.application.Exceptions.ValidationErrorException>(() =>
            StaffMemberProfileValidationHelper.ValidateDateRange(
                new DateTime(2026, 2, 1),
                new DateTime(2026, 1, 31)));
    }

    [Test]
    public void Shared_profile_validation_accepts_an_open_end_date()
    {
        Assert.DoesNotThrow(() =>
            StaffMemberProfileValidationHelper.ValidateDateRange(
                new DateTime(2026, 2, 1),
                null));
    }

    [Test]
    public void Shared_profile_validation_requires_gap_details_only_when_gap_is_selected()
    {
        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(() =>
                StaffMemberProfileValidationHelper.ValidateGap(false, 0, null, "Education gap"));
            Assert.Throws<skolenza.application.Exceptions.ValidationErrorException>(() =>
                StaffMemberProfileValidationHelper.ValidateGap(true, 0, null, "Education gap"));
            Assert.Throws<skolenza.application.Exceptions.ValidationErrorException>(() =>
                StaffMemberProfileValidationHelper.ValidateGap(true, 1, " ", "Education gap"));
        });
    }

    #endregion

    #region Permission pipeline coverage

    /// <summary>
    /// Verifies every concrete StaffMemberCmd request resolves to a server-owned leaf module and carries
    /// a permission DTO through its request, DTO, Filter, or PermissionRequest contract.
    /// </summary>
    [Test]
    public void Every_staffMember_request_is_mapped_to_a_leaf_module_and_carries_permission_identifiers()
    {
        var requestTypes = typeof(StaffMemberInstitutionPermissionBehavior<,>).Assembly
            .GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract && type.Namespace?.StartsWith(
                "skolenza.application.Features.StaffMemberCmd",
                StringComparison.Ordinal) == true)
            .Where(type => type.GetInterfaces().Any(contract =>
                contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IRequest<>)))
            // These read-only dropdowns intentionally authenticate the Institution token only. They do
            // not represent module operations and never accept staffMember identifiers from callers.
            .Where(type => type.FullName is not
                "skolenza.application.Features.StaffMemberCmd.StaffMemberWorkInfo.Handlers.GetAttendancePolicyOptionsQuery" and not
                "skolenza.application.Features.StaffMemberCmd.Contact.Handlers.GetContactRelationOptionsQuery")
            .OrderBy(type => type.FullName)
            .ToArray();

        var failures = new List<string>();
        foreach (var requestType in requestTypes)
        {
            var behaviorType = typeof(StaffMemberInstitutionPermissionBehavior<,>)
                .MakeGenericType(requestType, typeof(object));
            var resolver = behaviorType.GetMethod(
                "ResolveExpectedModuleCode",
                BindingFlags.NonPublic | BindingFlags.Static);
            var moduleCode = resolver?.Invoke(null, null) as string;
            var carriesPermission = CarriesPermissionRequest(requestType);

            if (string.IsNullOrWhiteSpace(moduleCode) || !carriesPermission)
            {
                failures.Add($"{requestType.FullName}: module={moduleCode ?? "<none>"}, permissionDTO={carriesPermission}");
            }
        }

        Assert.That(requestTypes, Is.Not.Empty);
        Assert.That(failures, Is.Empty,
            "StaffMember permission pipeline gaps:\n" + string.Join("\n", failures));
    }

    #endregion

    #region API and DTO contracts

    /// <summary>
    /// Verifies the profile-status endpoint remains an authenticated GET with its current route.
    /// </summary>
    [Test]
    public void Get_all_percentage_route_and_response_envelope_remain_explicit()
    {
        var action = typeof(StaffController).GetMethod(
            nameof(StaffController.GetAllStaffMemberPercentageAsync));
        var route = action?.GetCustomAttributes<HttpMethodAttribute>().Single();
        var returnContract = typeof(ApiResponse<List<CompletionSectionDTO>>);

        Assert.Multiple(() =>
        {
            Assert.That(route?.HttpMethods, Does.Contain("GET"));
            Assert.That(route?.Template, Is.EqualTo("get-all-percentage"));
            Assert.That(action?.GetCustomAttributes().Any(attribute =>
                attribute.GetType().Name == "AuthorizeAttribute"), Is.True);
            Assert.That(returnContract.GetProperty(nameof(ApiResponse<object>.Data)), Is.Not.Null);
            Assert.That(returnContract.GetProperty("Sections"), Is.Null,
                "The backend currently returns the section list in ApiResponse.Data.");
        });
    }

    /// <summary>
    /// Verifies the current shared section enum has only the original eight sections.
    /// </summary>
    [Test]
    public void Tab_info_type_keeps_the_current_seven_section_contract()
    {
        Assert.That(Enum.GetNames<TabInfoType>(), Is.EqualTo(new[]
        {
            "StaffMember", "Bank", "Contact", "Experience", "Identity", "Education", "Dependent"
        }));
    }

    [Test]
    public void Profile_status_exposes_update_bulk_ids_only_for_persisted_verification_sections()
    {
        var names = new[]
        {
            "Overview", "Bank", "Contact", "Experience", "Identity", "Education",
            "Dependent", "Work Locations", "Devices", "Work Arrangement", "Work Pattern", "Overrides"
        };
        var sections = StaffMemberProfileCompletionCalculator.ApplyVerificationContract(
            names.Select(name => new CompletionSectionDTO { SectionName = name }).ToList());

        Assert.Multiple(() =>
        {
            Assert.That(sections.Where(section => section.CanUpdateVerificationStatus)
                .Select(section => section.TabInfoType), Is.EqualTo(new int?[] { 1, 2, 3, 4, 5, 6, 7 }));
            Assert.That(sections.Where(section => !section.CanUpdateVerificationStatus)
                .Select(section => section.SectionName), Is.EqualTo(new[]
                { "Work Locations", "Devices", "Work Arrangement", "Work Pattern", "Overrides" }));
            Assert.That(sections.Where(section => !section.CanUpdateVerificationStatus)
                .All(section => section.TabInfoType is null), Is.True);
        });
    }

    /// <summary>
    /// Verifies each legacy StaffMember profile controller keeps its currently published CRUD routes.
    /// </summary>
    [TestCase(typeof(BankController), "POST:create", "GET:get", "POST:update", "DELETE:delete")]
    [TestCase(typeof(ContactController), "GET:relation-options", "POST:create", "GET:get", "POST:update", "DELETE:delete")]
    [TestCase(typeof(EducationController), "POST:create", "GET:get", "POST:update-education", "DELETE:delete")]
    [TestCase(typeof(ExperienceController), "POST:create", "GET:get", "POST:update", "DELETE:delete", "DELETE:delete-doc")]
    [TestCase(typeof(DependentController), "POST:create", "GET:get", "GET:get-in-detail", "POST:update", "DELETE:delete")]
    [TestCase(typeof(SensitiveController), "POST:Create", "GET:get", "DELETE:delete")]
    public void Legacy_profile_controller_keeps_its_current_route_surface(
        Type controllerType,
        params string[] expectedRoutes)
    {
        var actualRoutes = controllerType
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .SelectMany(method => method.GetCustomAttributes<HttpMethodAttribute>())
            .Select(attribute => $"{attribute.HttpMethods.Single()}:{attribute.Template}")
            .ToArray();

        Assert.That(actualRoutes, Is.EquivalentTo(expectedRoutes));
    }

    [Test]
    public void Identity_projection_keeps_distinct_effective_from_and_to_dates()
    {
        var source = new[]
        {
            new GetStaffMemberIdentitySp
            {
                StaffMemberId = 42,
                EffectiveFrom = new DateOnly(2026, 1, 2),
                EffectiveTo = new DateOnly(2031, 3, 4)
            }
        };
        var result = ProjectionHelper
            .ToGetIdentityResponseDTO(source, new TestIdEncoder(), "institution-key")
            .Single();

        Assert.Multiple(() =>
        {
            Assert.That(result.StaffMemberId, Is.EqualTo("42"));
            Assert.That(result.EffectiveFrom, Is.EqualTo(new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc)));
            Assert.That(result.EffectiveTo, Is.EqualTo(new DateTime(2031, 3, 4, 0, 0, 0, DateTimeKind.Utc)));
        });
    }

    [Test]
    public void Identity_projection_calculates_each_catalogue_row_from_value_and_document()
    {
        var source = new[]
        {
            new GetStaffMemberIdentitySp
            {
                StaffMemberIdentityId = 1,
                IdentityValue = "ABC123",
                HasIdentityUploaded = true
            },
            new GetStaffMemberIdentitySp
            {
                StaffMemberIdentityId = 2,
                IdentityValue = "   ",
                HasIdentityUploaded = false
            },
            new GetStaffMemberIdentitySp
            {
                IdentityValue = null,
                IsMandatory = true
            }
        };

        var result = ProjectionHelper
            .ToGetIdentityResponseDTO(source, new TestIdEncoder(), "institution-key")
            .Select(item => item.CompletionPercentage)
            .ToArray();

        Assert.That(result, Is.EqualTo(new[] { 100d, 0d, 0d }));
    }

    #endregion

    #region Test helpers

    private static void AssertEmpty(CompletionSectionDTO section, string expectedName)
    {
        Assert.Multiple(() =>
        {
            Assert.That(section.SectionName, Is.EqualTo(expectedName));
            Assert.That(section.CompletionPercent, Is.EqualTo(0));
            Assert.That(section.IsInfoVerified, Is.False);
            Assert.That(section.IsEditAllowed, Is.False);
            Assert.That(section.IsSectionCreate, Is.False);
        });
    }

    private static bool CarriesPermissionRequest(Type requestType)
    {
        foreach (var propertyName in new[] { "DTO", "Filter", "PermissionRequest" })
        {
            var property = requestType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (property is not null && typeof(PermissionRequestDTO).IsAssignableFrom(property.PropertyType))
            {
                return true;
            }

            var field = requestType.GetField(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (field is not null && typeof(PermissionRequestDTO).IsAssignableFrom(field.FieldType))
            {
                return true;
            }
        }

        return false;
    }

    private sealed class TestIdEncoder : IIdEncoderService
    {
        public string EncodeId_long(long id, string institutionKey) => id.ToString();
        public long DecodeId_long(string? encodedId, string institutionKey) => long.Parse(encodedId!);
        public int DecodeId_int(string? encodedId, string institutionKey) => int.Parse(encodedId!);
        public string EncodeId_int(int id, string institutionKey) => id.ToString();
        public string EncodeString(string input, string institutionKey) => input;
        public string DecodeString(string input, string institutionKey) => input;
    }

    #endregion
}

