using System.Reflection;
using skolenza.api.Controllers.Policies;
using skolenza.application.DTOS.Policy;
using skolenza.application.Features.GenericPolicyCmd;
using skolenza.application.Common.Enums;
using skolenza.application.Constants;
using skolenza.domain.Entity;
using skolenza.infrastructure.EncryptionService;
using skolenza.persistance.Data.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("PolicyFramework")]
public sealed class GenericPolicyApiContractTests
{
    [Test]
    public void StaffMember_long_identifier_round_trips_with_the_global_salt_independent_of_institution_key()
    {
        var encoder = new IdEncoderService();
        const long staffMemberId = 12345;

        var encodedWithFirstKey = encoder.EncodeId_long(staffMemberId, "institution-key-one");
        var encodedWithSecondKey = encoder.EncodeId_long(staffMemberId, "institution-key-two");

        Assert.Multiple(() =>
        {
            Assert.That(encodedWithFirstKey, Is.Not.Empty);
            Assert.That(encodedWithSecondKey, Is.EqualTo(encodedWithFirstKey));
            Assert.That(encoder.DecodeId_long(encodedWithFirstKey, string.Empty), Is.EqualTo(staffMemberId));
        });
    }

    [Test]
    public void Resolve_staffMember_identifier_uses_the_encoded_public_contract()
    {
        Assert.Multiple(() =>
        {
            Assert.That(typeof(ResolveStaffMemberPoliciesRequestDTO)
                .GetProperty(nameof(ResolveStaffMemberPoliciesRequestDTO.StaffMemberId))?.PropertyType,
                Is.EqualTo(typeof(string)));
            Assert.That(typeof(ResolveStaffMemberPoliciesRequestDTO).GetProperty("ResolvedStaffMemberId"), Is.Null);
        });

        var handler = ReadRepositoryFile(
            "skolenza.application",
            "Features",
            "GenericPolicyCmd",
            "GenericPolicyHandlers.cs");
        Assert.Multiple(() =>
        {
            Assert.That(handler, Does.Contain("idEncoderService.DecodeId_long("));
            Assert.That(handler, Does.Contain("EncryptionSanitizer.CleanEncodedInput(request.DTO.StaffMemberId)"));
            Assert.That(handler, Does.Contain("actor.Claims.InstitutionEncriptionKey"));
        });
    }

    [Test]
    public void Assign_staffMember_identifiers_use_the_encoded_public_contract()
    {
        Assert.Multiple(() =>
        {
            Assert.That(typeof(AssignPolicyRequestDTO)
                .GetProperty(nameof(AssignPolicyRequestDTO.StaffMemberIds))?.PropertyType,
                Is.EqualTo(typeof(List<string>)));
            Assert.That(typeof(AssignPolicyRequestDTO)
                .GetProperty(nameof(AssignPolicyRequestDTO.ResolvedStaffMemberIds)), Is.Not.Null);
        });

        var handler = ReadRepositoryFile(
            "skolenza.application",
            "Features",
            "GenericPolicyCmd",
            "GenericPolicyHandlers.cs");
        var repository = ReadRepositoryFile(
            "skolenza.persistance",
            "Repositories",
            "GenericPolicyRepository.cs");

        Assert.Multiple(() =>
        {
            Assert.That(handler, Does.Contain("request.DTO.ResolvedStaffMemberIds = request.DTO.StaffMemberIds"));
            Assert.That(handler, Does.Contain("idEncoderService.DecodeId_long("));
            Assert.That(handler, Does.Contain("EncryptionSanitizer.CleanEncodedInput(id)"));
            Assert.That(repository, Does.Contain("dto.ResolvedStaffMemberIds.Distinct().ToList()"));
        });
    }

    [Test]
    public void Policy_audit_contract_includes_server_resolved_actor_name_and_version_number()
    {
        Assert.Multiple(() =>
        {
            Assert.That(typeof(PolicyAuditResponseDTO)
                .GetProperty(nameof(PolicyAuditResponseDTO.ChangedByName))?.PropertyType,
                Is.EqualTo(typeof(string)));
            Assert.That(typeof(PolicyAuditResponseDTO)
                .GetProperty(nameof(PolicyAuditResponseDTO.VersionNumber))?.PropertyType,
                Is.EqualTo(typeof(int?)));
        });

        var repository = ReadRepositoryFile(
            "skolenza.persistance",
            "Repositories",
            "GenericPolicyRepository.cs");

        Assert.Multiple(() =>
        {
            Assert.That(repository,
                Does.Contain("context.StaffMembers.AsNoTracking().Where(x => x.InstitutionId == institutionId)"));
            Assert.That(repository,
                Does.Contain(".Where(x => x.InstitutionId == institutionId && x.PolicyId == policyId)"));
            Assert.That(repository, Does.Contain("BuildStaffMemberDisplayName("));
        });
    }

    [Test]
    public void Route_supplied_identifiers_skip_pre_action_model_validation()
    {
        var routeProperties = new (Type Type, string Property)[]
        {
            (typeof(PolicyByIdRequestDTO), nameof(PolicyByIdRequestDTO.Id)),
            (typeof(UpdateGenericPolicyTypeRequestDTO), nameof(UpdateGenericPolicyTypeRequestDTO.Id)),
            (typeof(ChangePolicyTypeStatusRequestDTO), nameof(ChangePolicyTypeStatusRequestDTO.Id)),
            (typeof(UpdatePolicyDraftRequestDTO), nameof(UpdatePolicyDraftRequestDTO.PolicyId)),
            (typeof(UpdatePolicyDraftRequestDTO), nameof(UpdatePolicyDraftRequestDTO.PolicyVersionId)),
            (typeof(ClonePolicyVersionRequestDTO), nameof(ClonePolicyVersionRequestDTO.PolicyId)),
            (typeof(PolicyTransitionRequestDTO), nameof(PolicyTransitionRequestDTO.PolicyVersionId)),
            (typeof(RemovePolicyAssignmentRequestDTO), nameof(RemovePolicyAssignmentRequestDTO.AssignmentId)),
            (typeof(ApprovePolicyExceptionRequestDTO), nameof(ApprovePolicyExceptionRequestDTO.ExceptionId)),
            (typeof(PolicyDocumentsRequestDTO), nameof(PolicyDocumentsRequestDTO.PolicyVersionId)),
            (typeof(DeletePolicyDocumentRequestDTO), nameof(DeletePolicyDocumentRequestDTO.DocumentId)),
            (typeof(PolicyVersionAccessRequestDTO), nameof(PolicyVersionAccessRequestDTO.PolicyVersionId)),
            (typeof(PolicyAssignmentCandidateRequestDTO), nameof(PolicyAssignmentCandidateRequestDTO.PolicyVersionId)),
            (typeof(UpdatePolicyApprovalStageRequestDTO), nameof(UpdatePolicyApprovalStageRequestDTO.Id)),
            (typeof(DeletePolicyApprovalStageRequestDTO), nameof(DeletePolicyApprovalStageRequestDTO.Id))
        };

        Assert.Multiple(() =>
        {
            foreach (var (type, propertyName) in routeProperties)
            {
                var property = type.GetProperty(propertyName);
                Assert.That(property, Is.Not.Null, $"{type.Name}.{propertyName}");
                Assert.That(property!.GetCustomAttributes<System.ComponentModel.DataAnnotations.ValidationAttribute>(), Is.Empty,
                    $"{type.Name}.{propertyName} is supplied by the route after automatic DTO validation.");
            }
        });
    }

    [Test]
    public void Ef_model_maps_all_generic_policy_tables_and_json_columns()
    {
        var options = new DbContextOptionsBuilder<SkolenzaDbContext>().UseNpgsql("Host=localhost;Database=model_only;Username=x;Password=x").Options;
        using var context = new SkolenzaDbContext(options);
        var expected = new[] { typeof(PolicyCategory), typeof(PolicyStatus), typeof(PolicyRuleType), typeof(PolicyDocumentType), typeof(PolicyType), typeof(Policy), typeof(PolicyVersion), typeof(PolicyRule), typeof(PolicyVersionLeaveType), typeof(PolicyRuleLeaveType), typeof(PolicyApplicability), typeof(PolicyApplicabilityLeaveType), typeof(PolicyAssignment), typeof(PolicyException), typeof(PolicyDocument), typeof(PolicyApprovalStage), typeof(PolicyApprovalHistory), typeof(PolicyAcknowledgement), typeof(PolicyChangeAudit) };
        Assert.Multiple(() =>
        {
            foreach (var type in expected) Assert.That(context.Model.FindEntityType(type), Is.Not.Null, type.Name);
            Assert.That(context.Model.FindEntityType(typeof(PolicyRule))!.FindProperty(nameof(PolicyRule.RuleConfiguration))!.GetColumnType(), Is.EqualTo("jsonb"));
            Assert.That(context.Model.FindEntityType(typeof(PolicyException))!.FindProperty(nameof(PolicyException.OverrideConfiguration))!.GetColumnType(), Is.EqualTo("jsonb"));
        });
    }

    [Test]
    public void Leave_policy_contract_targets_versions_rules_and_applicability_by_leave_type()
    {
        Assert.Multiple(() =>
        {
            Assert.That(typeof(CreatePolicyRequestDTO).GetProperty(nameof(CreatePolicyRequestDTO.LeaveTypeIds)), Is.Not.Null);
            Assert.That(typeof(PolicyRuleInputDTO).GetProperty(nameof(PolicyRuleInputDTO.LeaveTypeIds)), Is.Not.Null);
            Assert.That(typeof(PolicyApplicabilityInputDTO).GetProperty(nameof(PolicyApplicabilityInputDTO.LeaveTypeIds)), Is.Not.Null);
            Assert.That(typeof(ResolveStaffMemberPoliciesRequestDTO).GetProperty(nameof(ResolveStaffMemberPoliciesRequestDTO.LeaveTypeId)), Is.Not.Null);
            Assert.That(typeof(PolicyDetailResponseDTO).GetProperty(nameof(PolicyDetailResponseDTO.LeaveTypes)), Is.Not.Null);
        });
        var repository = ReadRepositoryFile("skolenza.persistance", "Repositories", "GenericPolicyRepository.cs");
        Assert.Multiple(() =>
        {
            Assert.That(repository, Does.Contain("Every Leave rule must target one or more covered Leave Types."));
            Assert.That(repository, Does.Contain("Every Leave applicability row must target one or more covered Leave Types."));
            Assert.That(repository, Does.Contain("VersionLeaveTypes = versionLeaveTypes"));
        });
    }

    [Test]
    public void Policy_identity_rejects_duplicate_institution_name_and_preserves_per_policy_versioning()
    {
        var repository = ReadRepositoryFile(
            "skolenza.persistance",
            "Repositories",
            "GenericPolicyRepository.cs");
        var migration = ReadRepositoryFile(
            "database-scripts",
            "EnforceInstitutionPolicyUniqueName.sql");

        Assert.Multiple(() =>
        {
            Assert.That(repository,
                Does.Contain("A policy named '{name}' already exists. Open the existing policy and create its next version instead."));
            Assert.That(repository, Does.Contain("x.PolicyName.Trim().ToUpper() == normalizedName"));
            Assert.That(migration, Does.Contain("UX_Policy_Institution_NormalizedName"));
            Assert.That(migration, Does.Contain("lower(btrim(\"PolicyName\"))"));
            Assert.That(migration, Does.Contain("GROUP BY \"InstitutionId\", lower(btrim(\"PolicyName\"))"));
        });
    }

    [Test]
    public void Policy_document_handler_passes_an_extension_free_generated_name_to_storage()
    {
        var handler = ReadRepositoryFile(
            "skolenza.application",
            "Features",
            "GenericPolicyCmd",
            "GenericPolicyHandlers.cs");

        Assert.Multiple(() =>
        {
            Assert.That(handler, Does.Contain("var storedName = $\"{Guid.NewGuid():N}\";"));
            Assert.That(handler, Does.Not.Contain("var storedName = $\"{Guid.NewGuid():N}{extension}\";"));
            Assert.That(handler, Does.Contain("fileStorageService.UploadFileAsync(file"));
        });
    }

    [Test]
    public void Controller_exposes_policy_crud_lifecycle_resolution_assignment_exception_and_acknowledgement_routes()
    {
        var routes = typeof(InstitutionPolicyController).GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .SelectMany(method => method.GetCustomAttributes<HttpMethodAttribute>().Select(attribute => $"{attribute.HttpMethods.Single()}:{attribute.Template}"))
            .ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(routes, Does.Contain("GET:lookups"));
            Assert.That(routes, Does.Contain("POST:"));
            Assert.That(routes, Does.Contain("PUT:{policyId:long}/versions/{versionId:long}"));
            Assert.That(routes, Does.Contain("POST:versions/{versionId:long}/transition"));
            Assert.That(routes, Does.Contain("GET:resolve"));
            Assert.That(routes, Does.Contain("POST:assignments"));
            Assert.That(routes, Does.Contain("POST:exceptions"));
            Assert.That(routes, Does.Contain("POST:acknowledgements"));
            Assert.That(routes, Does.Contain("GET:approval-stages"));
            Assert.That(routes, Does.Contain("POST:approval-stages"));
            Assert.That(routes, Does.Contain("GET:versions/{versionId:long}/approval-progress"));
            Assert.That(routes, Does.Contain("GET:versions/{versionId:long}/assignments"));
            Assert.That(routes, Does.Contain("GET:versions/{versionId:long}/assignment-candidates"));
            Assert.That(routes, Does.Contain("GET:versions/{versionId:long}/assignments/export"));
            Assert.That(routes, Does.Contain("GET:versions/{versionId:long}/exceptions"));
            Assert.That(routes, Does.Contain("GET:versions/{versionId:long}/acknowledgements"));
            Assert.That(routes, Does.Contain("POST:bulk/{target}/preview"));
            Assert.That(routes, Does.Contain("POST:bulk/{target}/confirm"));
            Assert.That(routes, Does.Contain("GET:bulk/{target}/jobs/{jobId:guid}"));
            Assert.That(routes, Does.Contain("GET:bulk/{target}/jobs"));
            Assert.That(routes, Does.Contain("POST:bulk/{target}/retry"));
            Assert.That(routes, Does.Contain("POST:bulk/{target}/cancel"));
            Assert.That(routes, Does.Contain("GET:bulk/{target}/template"));
            Assert.That(routes, Does.Contain("GET:bulk/{target}/jobs/{jobId:guid}/report"));
        });
    }

    [Test]
    public void Policy_lookups_publish_every_attendance_location_scope_from_the_domain_enum()
    {
        var source = ReadRepositoryFile(
            "skolenza.application",
            "Features",
            "GenericPolicyCmd",
            "GenericPolicyHandlers.cs");

        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Contain("Enum.GetValues<AttendanceLocationScope>()"));
            Assert.That(source, Does.Contain("attendanceLocationScopes"));
            Assert.That(Enum.GetValues<AttendanceLocationScope>(), Has.Length.EqualTo(4));
            Assert.That((short)AttendanceLocationScope.PrimaryLocationOnly, Is.EqualTo(1));
            Assert.That((short)AttendanceLocationScope.AssignedLocations, Is.EqualTo(2));
            Assert.That((short)AttendanceLocationScope.AnyCampus, Is.EqualTo(3));
            Assert.That((short)AttendanceLocationScope.RemoteAnywhere, Is.EqualTo(4));
        });
    }

    [Test]
    public void Policy_bulk_targets_have_stable_master_values_and_exact_templates()
    {
        Assert.Multiple(() =>
        {
            Assert.That((int)BulkImportMaster.PolicyType, Is.EqualTo(12));
            Assert.That((int)BulkImportMaster.PolicyDefinition, Is.EqualTo(13));
            Assert.That((int)BulkImportMaster.PolicyAssignment, Is.EqualTo(14));
            Assert.That(BulkImportConstants.PolicyTypeColumns,
                Is.EqualTo(new[] { "PolicyTypeCode", "PolicyName", "PolicyCategoryCode", "Description", "DefaultCurrencyCode", "IsActive" }));
            Assert.That(BulkImportConstants.PolicyDefinitionColumns,
                Does.Contain("RulesJson").And.Contain("ApplicabilityJson"));
            Assert.That(BulkImportConstants.PolicyAssignmentColumns,
                Is.EqualTo(new[] { "PolicyCode", "VersionNumber", "StaffMemberCode", "EffectiveFrom", "EffectiveTo", "IsMandatory" }));
        });
    }

    [Test]
    public void Policy_bulk_migration_allows_all_policy_masters_and_is_published()
    {
        var migration = ReadRepositoryFile("database-scripts", "AddPolicyBulkImport.sql");
        var runner = ReadRepositoryFile("database-scripts", "ApplyBulkImportMigrations.ps1");
        var project = ReadRepositoryFile("skolenza.api", "skolenza.api.csproj");
        Assert.Multiple(() =>
        {
            Assert.That(migration, Does.Contain("CHECK (\"Master\" BETWEEN 1 AND 14)"));
            Assert.That(runner, Does.Contain("[switch] $PolicyBulkOnly"));
            Assert.That(runner, Does.Contain("$scripts = @('AddPolicyBulkImport.sql')"));
            Assert.That(project, Does.Contain("AddPolicyBulkImport.sql"));
        });
    }

    [Test]
    public void Policy_bulk_permission_and_worker_are_bound_to_existing_leaf_modules()
    {
        var permission = ReadRepositoryFile("skolenza.application", "Features", "GenericPolicyCmd", "GenericPolicyPermissionBehavior.cs");
        var worker = ReadRepositoryFile("skolenza.persistance", "Repositories", "BulkImportRepository.cs");
        Assert.Multiple(() =>
        {
            Assert.That(permission, Does.Contain("BulkImportMaster.PolicyType => \"INSTITUTION_POLICY_TYPES\""));
            Assert.That(permission, Does.Contain("BulkImportMaster.PolicyDefinition => \"INSTITUTION_POLICY_DEFINITIONS\""));
            Assert.That(permission, Does.Contain("BulkImportMaster.PolicyAssignment => \"INSTITUTION_POLICY_ASSIGNMENTS\""));
            Assert.That(worker, Does.Contain("await ProcessPolicyBatchAsync(job, preview, cancellationToken)"));
            Assert.That(worker, Does.Contain("BulkImportMaster.PolicyAssignment => BulkImportConstants.PolicyAssignmentModuleCode"));
        });
    }

    [Test]
    public void Policy_bulk_revalidates_rows_and_enforces_institution_reference_boundaries()
    {
        var source = ReadRepositoryFile("skolenza.persistance", "Repositories", "BulkImportRepository.Policy.cs");
        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Contain("BuildPolicyPreviewAsync((BulkImportMaster)job.Master"));
            Assert.That(source, Does.Contain("RulesJson contains an inactive or unknown PolicyRuleTypeId."));
            Assert.That(source, Does.Contain("CampusId outside the institution or supplied geography."));
            Assert.That(source, Does.Contain("StaffEmploymentTypeId for this institution."));
            Assert.That(source, Does.Contain("PolicyCode and VersionNumber must identify a published institution policy version."));
        });
    }

    [Test]
    public void Policy_bulk_definition_creates_draft_and_assignment_creates_acknowledgement()
    {
        var source = ReadRepositoryFile("skolenza.persistance", "Repositories", "BulkImportRepository.Policy.cs");
        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Contain("PolicyStatusId = draftStatusId"));
            Assert.That(source, Does.Contain("AppConstants.PolicyStatusCodes.Draft"));
            Assert.That(source, Does.Contain("AppConstants.PolicyStatusCodes.Published"));
            Assert.That(source, Does.Not.Contain("candidate.PolicyStatusId == 4"));
            Assert.That(source, Does.Contain("ActionName = \"BULK_CREATE\""));
            Assert.That(source, Does.Contain("context.PolicyAcknowledgements.Add"));
            Assert.That(source, Does.Contain("AcknowledgementStatus = 1"));
        });
    }

    [Test]
    public void Ui_catalog_documents_every_controller_operation_with_request_and_response_examples()
    {
        var catalog = ReadRepositoryFile("docs", "UIdeveloperDoc", "INSTITUTION_POLICY_ENDPOINT_CATALOG.md");
        var methods = typeof(InstitutionPolicyController).GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .SelectMany(method => method.GetCustomAttributes<HttpMethodAttribute>())
            .ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(methods, Has.Length.EqualTo(40));
            for (var number = 1; number <= methods.Length; number++)
            {
                Assert.That(catalog, Does.Contain($"### {number}."), $"Missing documented endpoint number {number}");
            }
            Assert.That(catalog, Does.Contain("Input body"));
            Assert.That(catalog, Does.Contain("Output sample"));
            Assert.That(catalog, Does.Contain("Current verification status"));
        });
    }

    [Test]
    public void Every_generic_policy_request_carries_dynamic_permission_ids()
    {
        var requestTypes = typeof(CreatePolicyRequestDTO).Assembly.GetTypes().Where(x => x.Namespace == typeof(CreatePolicyRequestDTO).Namespace && x.Name.EndsWith("RequestDTO", StringComparison.Ordinal)).ToArray();
        Assert.That(requestTypes, Is.Not.Empty);
        Assert.Multiple(() => { foreach (var type in requestTypes) { Assert.That(type.GetProperty("ModuleId"), Is.Not.Null, type.Name); Assert.That(type.GetProperty("OperationId"), Is.Not.Null, type.Name); } });
    }

    [Test]
    public void Lifecycle_contract_supports_only_documented_actions()
    {
        var source = ReadRepositoryFile("skolenza.persistance", "Repositories", "GenericPolicyRepository.cs");
        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Contain("current == statusIds.Draft => statusIds.UnderReview"));
            Assert.That(source, Does.Contain("RecordApprovalDecisionAsync"));
            Assert.That(source, Does.Contain("currentStage.MinimumApprovals"));
            Assert.That(source, Does.Contain("currentStage.ApproverRoleId"));
            Assert.That(source, Does.Contain("x.ActionById == actorId"));
            Assert.That(source, Does.Contain("current == statusIds.Approved => statusIds.Published"));
            Assert.That(source, Does.Contain("current == statusIds.Published => statusIds.Archived"));
            Assert.That(source, Does.Contain("AppConstants.PolicyStatusCodes.Draft"));
            Assert.That(source, Does.Not.Contain("private const short Draft = 1"));
            Assert.That(source, Does.Contain("Only a published policy version can be assigned."));
        });
    }

    [Test]
    public void Draft_update_flushes_removed_version_graph_before_inserting_replacement_rows()
    {
        var source = ReadRepositoryFile("skolenza.persistance", "Repositories", "GenericPolicyRepository.cs");
        var updateStart = source.IndexOf(
            "public async Task<PolicyDetailResponseDTO> UpdateDraftAsync",
            StringComparison.Ordinal);
        var cloneStart = source.IndexOf(
            "public async Task<PolicyDetailResponseDTO> CloneVersionAsync",
            updateStart,
            StringComparison.Ordinal);
        var update = source[updateStart..cloneStart];
        var removeRules = update.IndexOf("context.PolicyRules.RemoveRange", StringComparison.Ordinal);
        var deleteFlush = update.IndexOf("await context.SaveChangesAsync(cancellationToken);", removeRules, StringComparison.Ordinal);
        var addRules = update.IndexOf("AddRulesAndApplicability", deleteFlush, StringComparison.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(removeRules, Is.GreaterThanOrEqualTo(0));
            Assert.That(deleteFlush, Is.GreaterThan(removeRules));
            Assert.That(addRules, Is.GreaterThan(deleteFlush));
            Assert.That(update, Does.Contain("await using var transaction"));
        });
    }

    [Test]
    public void Publication_requires_staffMember_visible_policy_document_and_approved_content_fingerprint()
    {
        var source = ReadRepositoryFile(
            "skolenza.persistance",
            "Repositories",
            "GenericPolicyRepository.cs");
        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Contain("action is \"SUBMIT\" or \"PUBLISH\""));
            Assert.That(source, Does.Contain("documentType.DocumentTypeCode == \"POLICY_DOCUMENT\""));
            Assert.That(source, Does.Contain("document.IsStaffMemberVisible"));
            Assert.That(source, Does.Contain("ComputeVersionContentChecksumAsync("));
            Assert.That(source, Does.Contain("version.ApprovedContentChecksumSha256"));
            Assert.That(source, Does.Contain("Documents can be changed only on draft or rejected versions."));
            Assert.That(typeof(PolicyVersion)
                .GetProperty(nameof(PolicyVersion.ApprovedContentChecksumSha256)), Is.Not.Null);
            Assert.That(typeof(PolicyApprovalHistory)
                .GetProperty(nameof(PolicyApprovalHistory.ContentChecksumSha256)), Is.Not.Null);
        });
    }

    [Test]
    public void Clone_creates_a_draft_copy_of_rules_scopes_and_attendance_without_evidence_records()
    {
        var source = ReadRepositoryFile(
            "skolenza.persistance",
            "Repositories",
            "GenericPolicyRepository.cs");
        var cloneStart = source.IndexOf("public async Task<PolicyDetailResponseDTO> CloneVersionAsync", StringComparison.Ordinal);
        var transitionStart = source.IndexOf("public async Task<PolicyDetailResponseDTO> TransitionAsync", cloneStart, StringComparison.Ordinal);
        var clone = source[cloneStart..transitionStart];

        Assert.Multiple(() =>
        {
            Assert.That(clone, Does.Contain("PolicyStatusId = statusIds.Draft"));
            Assert.That(clone, Does.Contain("context.PolicyRules.AddRange"));
            Assert.That(clone, Does.Contain("context.PolicyApplicabilities.AddRange"));
            Assert.That(clone, Does.Contain("context.AttendancePolicyVersionConfigurations.Add"));
            Assert.That(clone, Does.Not.Contain("context.PolicyDocuments.Add"));
            Assert.That(clone, Does.Not.Contain("context.PolicyAssignments.Add"));
            Assert.That(clone, Does.Not.Contain("context.PolicyAcknowledgements.Add"));
            Assert.That(clone, Does.Not.Contain("context.PolicyApprovalHistories.Add"));
        });
    }

    [Test]
    public void Archived_version_preserves_assignment_rows_but_reports_them_as_ineffective()
    {
        var source = ReadRepositoryFile(
            "skolenza.persistance",
            "Repositories",
            "GenericPolicyRepository.cs");
        var handler = ReadRepositoryFile(
            "skolenza.application",
            "Features",
            "GenericPolicyCmd",
            "GenericPolicyHandlers.cs");

        Assert.Multiple(() =>
        {
            Assert.That(typeof(PolicyAssignmentResponseDTO)
                .GetProperty(nameof(PolicyAssignmentResponseDTO.IsEffective)), Is.Not.Null);
            Assert.That(typeof(PolicyAssignmentResponseDTO)
                .GetProperty(nameof(PolicyAssignmentResponseDTO.StateReason)), Is.Not.Null);
            Assert.That(source, Does.Contain("\"VERSION_ARCHIVED\""));
            Assert.That(source, Does.Contain("version.IsCurrent = false"));
            Assert.That(source, Does.Not.Contain("RemoveRange(context.PolicyAssignments"));
            Assert.That(handler, Does.Contain("IsActive,IsEffective,StateReason"));
        });
    }

    [Test]
    public void Repository_enforces_institution_scope_on_staffMember_sensitive_operations()
    {
        var source = ReadRepositoryFile("skolenza.persistance", "Repositories", "GenericPolicyRepository.cs");
        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Contain("x.InstitutionId == institutionId && staffMemberIds.Contains(x.Id)"));
            Assert.That(source, Does.Contain("x.Id == staffMemberId && x.InstitutionId == institutionId"));
            Assert.That(source, Does.Contain("x.Id == assignmentId && x.InstitutionId == institutionId"));
            Assert.That(source, Does.Contain("x.InstitutionId == institutionId && x.StaffMemberId == staffMemberId"));
        });
    }

    [Test]
    public void Assignment_creates_acknowledgement_idempotently_and_reactivates_removed_assignment()
    {
        var source = ReadRepositoryFile("skolenza.persistance", "Repositories", "GenericPolicyRepository.cs");
        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Contain("inactive.IsActive = true"));
            Assert.That(source, Does.Contain("context.PolicyAcknowledgements.AddRange"));
            Assert.That(source, Does.Contain("validStaffMemberIds.Except(acknowledgementStaffMemberIds)"));
        });
    }

    [Test]
    public void Assignment_picker_bulk_and_direct_mapping_enforce_the_same_applicability_boundary()
    {
        var repository = ReadRepositoryFile("skolenza.persistance", "Repositories", "GenericPolicyRepository.cs");
        var bulk = ReadRepositoryFile("skolenza.persistance", "Repositories", "BulkImportRepository.Policy.cs");
        var handler = ReadRepositoryFile("skolenza.application", "Features", "GenericPolicyCmd", "GenericPolicyHandlers.cs");
        Assert.Multiple(() =>
        {
            Assert.That(typeof(PolicyAssignmentCandidateResponseDTO)
                .GetProperty(nameof(PolicyAssignmentCandidateResponseDTO.StaffMemberCode)), Is.Not.Null);
            Assert.That(typeof(PolicyAssignmentCandidateResponseDTO)
                .GetProperty(nameof(PolicyAssignmentCandidateResponseDTO.DepartmentName)), Is.Not.Null);
            Assert.That(typeof(PolicyAssignmentCandidateResponseDTO)
                .GetProperty(nameof(PolicyAssignmentCandidateResponseDTO.DesignationName)), Is.Not.Null);
            Assert.That(repository, Does.Contain("GetApplicabilityEligibleStaffMemberIdsAsync("));
            Assert.That(repository, Does.Contain("outside the selected policy version applicability"));
            Assert.That(bulk, Does.Contain("outside the selected policy version applicability on EffectiveFrom"));
            Assert.That(handler, Does.Contain("StaffMemberCode,StaffMemberName,StaffEmploymentType,Department,Designation"));
            Assert.That(handler, Does.Contain("idEncoderService.EncodeId_long("));
        });
    }

    [Test]
    public void Applicability_validates_hierarchy_and_uses_documented_precedence()
    {
        var source = ReadRepositoryFile("skolenza.persistance", "Repositories", "GenericPolicyRepository.cs");
        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Contain("StateId does not belong to CountryId."));
            Assert.That(source, Does.Contain("DistrictId does not belong to StateId."));
            Assert.That(source, Does.Contain("CampusId does not match the supplied geography."));
            Assert.That(source, Does.Contain("var specificity = matching.Max(ScopeSpecificity)"));
            Assert.That(source, Does.Contain("var priority = mostSpecific.Min"));
            Assert.That(source, Does.Contain("winners.Any(x => x.ApplicabilityMode == 2)"));
        });
    }

    [Test]
    public void Permission_behavior_binds_each_action_group_to_its_leaf_module()
    {
        var source = ReadRepositoryFile("skolenza.application", "Features", "GenericPolicyCmd", "GenericPolicyPermissionBehavior.cs");
        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Contain("INSTITUTION_POLICY_TYPES"));
            Assert.That(source, Does.Contain("INSTITUTION_POLICY_DEFINITIONS"));
            Assert.That(source, Does.Contain("INSTITUTION_POLICY_ASSIGNMENTS"));
            Assert.That(source, Does.Contain("INSTITUTION_POLICY_EXCEPTIONS"));
            Assert.That(source, Does.Contain("INSTITUTION_POLICY_APPROVALS"));
            Assert.That(source, Does.Contain("INSTITUTION_POLICY_ACKNOWLEDGEMENTS"));
            Assert.That(source, Does.Contain("INSTITUTION_POLICY_AUDIT"));
            Assert.That(source, Does.Contain("The selected module is not valid for this policy action."));
        });
    }

    private static string ReadRepositoryFile(params string[] parts)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Skolenza.sln"))) directory = directory.Parent;
        return File.ReadAllText(Path.Combine(new[] { directory?.FullName ?? throw new DirectoryNotFoundException() }.Concat(parts).ToArray()));
    }
}

