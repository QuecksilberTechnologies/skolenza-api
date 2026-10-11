using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using skolenza.application.DTOs.BaseDTO;
using skolenza.domain.Entity;
using Microsoft.AspNetCore.Http;

namespace skolenza.application.DTOS.Policy;

public sealed class PolicyAccessRequestDTO : PermissionRequestDTO
{
    public bool IsActive { get; set; } = true;
}

public sealed class PolicyListRequestDTO : PermissionRequestDTO
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int? PolicyTypeId { get; set; }
    public short? StatusId { get; set; }
    public string? Search { get; set; }
}

public sealed class PolicyByIdRequestDTO : PermissionRequestDTO
{
    public long Id { get; set; }
}

public sealed class PolicyVersionByIdRequestDTO : PermissionRequestDTO
{
    public long PolicyId { get; set; }
    public long PolicyVersionId { get; set; }
}

public class CreateGenericPolicyTypeRequestDTO : PermissionRequestDTO
{
    [Required, MaxLength(50)] public string PolicyTypeCode { get; set; } = null!;
    [Required, MaxLength(150)] public string PolicyName { get; set; } = null!;
    [MaxLength(500)] public string? Description { get; set; }
    [Range(1, int.MaxValue)] public int PolicyCategoryId { get; set; }
    [StringLength(3, MinimumLength = 3)] public string? DefaultCurrencyCode { get; set; }
}

public sealed class UpdateGenericPolicyTypeRequestDTO : CreateGenericPolicyTypeRequestDTO
{
    public int Id { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class ChangePolicyTypeStatusRequestDTO : PermissionRequestDTO
{
    public int Id { get; set; }
    public bool IsActive { get; set; }
}

public class CreatePolicyRequestDTO : PermissionRequestDTO
{
    [Range(1, int.MaxValue)] public int PolicyTypeId { get; set; }
    [Required, MaxLength(50)] public string PolicyCode { get; set; } = null!;
    [Required, MaxLength(200)] public string PolicyName { get; set; } = null!;
    [MaxLength(1000)] public string? Summary { get; set; }
    public int? OwnerDepartmentId { get; set; }
    [StringLength(3, MinimumLength = 3)] public string? DefaultCurrencyCode { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    [MaxLength(1000)] public string? ChangeSummary { get; set; }
    public List<PolicyRuleInputDTO> Rules { get; set; } = new();
    public List<PolicyApplicabilityInputDTO> Applicability { get; set; } = new();
    public List<int> LeaveTypeIds { get; set; } = new();
    public AttendancePolicyVersionConfigurationDTO? AttendanceConfiguration { get; set; }
}

public sealed class AttendancePolicyVersionConfigurationDTO
{
    public AttendanceLocationScope AttendanceLocationScope { get; set; }
    public bool AllowBiometric { get; set; }
    public bool AllowMobile { get; set; }
    public bool AllowWeb { get; set; }
    public bool AllowManualAttendance { get; set; }
    public bool AllowWorkFromHome { get; set; }
    public bool RequireGeoFenceForOffice { get; set; }
    public bool RequireGpsForRemote { get; set; }
    public bool AllowOutsideLocationWithApproval { get; set; }
}

public sealed class UpdatePolicyDraftRequestDTO : CreatePolicyRequestDTO
{
    public long PolicyId { get; set; }
    public long PolicyVersionId { get; set; }
}

public sealed class PolicyRuleInputDTO
{
    [Range(1, int.MaxValue)] public int PolicyRuleTypeId { get; set; }
    [Required, MaxLength(150)] public string RuleName { get; set; } = null!;
    [Range(1, int.MaxValue)] public int RuleOrder { get; set; }
    [Required] public string RuleConfiguration { get; set; } = "{}";
    public List<int> LeaveTypeIds { get; set; } = new();
}

public sealed class PolicyApplicabilityInputDTO
{
    [Range(1, 2)] public short ApplicabilityMode { get; set; } = 1;
    public int? CountryId { get; set; }
    public int? StateId { get; set; }
    public int? DistrictId { get; set; }
    public int? LocalityId { get; set; }
    public long? CampusId { get; set; }
    public int? StaffEmploymentTypeId { get; set; }
    public int? DepartmentId { get; set; }
    public int? DesignationId { get; set; }
    public long? StaffMemberId { get; set; }
    /// <summary>Globally salted encoded staffMember identifier returned by the StaffMember APIs.</summary>
    public string? StaffMemberPublicId { get; set; }
    [JsonIgnore] public long? ResolvedStaffMemberId { get; set; }
    public int? GenderId { get; set; }
    public short? WorkArrangementType { get; set; }
    public short? EmploymentStatus { get; set; }
    [Range(0, int.MaxValue)] public int? MinimumServiceDays { get; set; }
    public int Priority { get; set; } = 100;
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public List<int> LeaveTypeIds { get; set; } = new();
}

public sealed class ClonePolicyVersionRequestDTO : PermissionRequestDTO
{
    public long PolicyId { get; set; }
    [Range(1, long.MaxValue)] public long SourceVersionId { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    [MaxLength(1000)] public string? ChangeSummary { get; set; }
}

public sealed class PolicyTransitionRequestDTO : PermissionRequestDTO
{
    public long PolicyVersionId { get; set; }
    [Required, MaxLength(20)] public string Action { get; set; } = null!;
    [MaxLength(1000)] public string? Comments { get; set; }
}

public sealed class ResolveStaffMemberPoliciesRequestDTO : PermissionRequestDTO
{
    /// <summary>Globally salted encoded staffMember identifier returned by the StaffMember APIs.</summary>
    [Required] public string StaffMemberId { get; set; } = string.Empty;
    public DateOnly? EffectiveDate { get; set; }
    public int? LeaveTypeId { get; set; }
}

public sealed class AssignPolicyRequestDTO : PermissionRequestDTO
{
    [Range(1, long.MaxValue)] public long PolicyVersionId { get; set; }
    /// <summary>Globally salted encoded staffMember identifiers returned by the StaffMember APIs.</summary>
    [MinLength(1)] public List<string> StaffMemberIds { get; set; } = new();
    [JsonIgnore] public List<long> ResolvedStaffMemberIds { get; set; } = new();
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsMandatory { get; set; } = true;
}

public sealed class RemovePolicyAssignmentRequestDTO : PermissionRequestDTO
{
    public long AssignmentId { get; set; }
}

public sealed class CreatePolicyExceptionRequestDTO : PermissionRequestDTO
{
    [Range(1, long.MaxValue)] public long PolicyVersionId { get; set; }
    /// <summary>Globally salted encoded staffMember identifier returned by the StaffMember APIs.</summary>
    [Required] public string StaffMemberId { get; set; } = string.Empty;
    [JsonIgnore] public long ResolvedStaffMemberId { get; set; }
    public short ExceptionType { get; set; }
    [Required] public string OverrideConfiguration { get; set; } = "{}";
    [Required, MaxLength(1000)] public string Reason { get; set; } = null!;
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly EffectiveTo { get; set; }
}

public sealed class ApprovePolicyExceptionRequestDTO : PermissionRequestDTO
{
    public long ExceptionId { get; set; }
    public bool Approve { get; set; }
}

public sealed class AcknowledgePolicyRequestDTO : PermissionRequestDTO
{
    [Range(1, long.MaxValue)] public long PolicyVersionId { get; set; }
    public string? EvidenceJson { get; set; }
}

public sealed class UploadPolicyDocumentRequestDTO : PermissionRequestDTO
{
    [Range(1, long.MaxValue)] public long PolicyVersionId { get; set; }
    [Range(1, short.MaxValue)] public short PolicyDocumentTypeId { get; set; }
    [Required, MaxLength(200)] public string DocumentTitle { get; set; } = null!;
    [MaxLength(10)] public string? LanguageCode { get; set; }
    public bool IsStaffMemberVisible { get; set; } = true;
    [Required] public IFormFile File { get; set; } = null!;
}

public sealed class PolicyDocumentsRequestDTO : PermissionRequestDTO
{
    public long PolicyVersionId { get; set; }
}

public sealed class DeletePolicyDocumentRequestDTO : PermissionRequestDTO
{
    public long DocumentId { get; set; }
}

public sealed class PolicyVersionAccessRequestDTO : PermissionRequestDTO
{
    public long PolicyVersionId { get; set; }
}

public sealed class PolicyAssignmentCandidateRequestDTO : PermissionRequestDTO
{
    public long PolicyVersionId { get; set; }
    public DateOnly? EffectiveDate { get; set; }
    public int? StaffEmploymentTypeId { get; set; }
    public int? DepartmentId { get; set; }
    public int? DesignationId { get; set; }
    [MaxLength(100)] public string? Search { get; set; }
    public bool IncludeAssigned { get; set; } = true;
    [Range(1, int.MaxValue)] public int PageNumber { get; set; } = 1;
    [Range(1, 200)] public int PageSize { get; set; } = 20;
}

public sealed class PolicyApprovalStageListRequestDTO : PermissionRequestDTO
{
    public int? PolicyCategoryId { get; set; }
    public bool IsActive { get; set; } = true;
}

public class CreatePolicyApprovalStageRequestDTO : PermissionRequestDTO
{
    public int? PolicyCategoryId { get; set; }
    [Required, MaxLength(100)] public string StageName { get; set; } = null!;
    [Range(1, int.MaxValue)] public int StageOrder { get; set; }
    public int? ApproverRoleId { get; set; }
    [Range(1, int.MaxValue)] public int MinimumApprovals { get; set; } = 1;
    public bool IsMandatory { get; set; } = true;
}

public sealed class UpdatePolicyApprovalStageRequestDTO : CreatePolicyApprovalStageRequestDTO
{
    public long Id { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class DeletePolicyApprovalStageRequestDTO : PermissionRequestDTO
{
    public long Id { get; set; }
}

public sealed record PolicyLookupResponseDTO(int Id, string Code, string Name, string? Description = null);

/// <summary>
/// Represents an enum-backed option published by the Policy lookup endpoint.
/// The numeric value comes directly from the domain enum so UI clients never maintain a second mapping.
/// </summary>
public sealed record PolicyEnumLookupResponseDTO(short Id, string Code, string Name);
public sealed record PolicyRuleSettingOptionResponseDTO(string Code, string Label, string ValueJson, int Order);
public sealed record PolicyRuleSettingDependencyResponseDTO(string DependsOnCode, string OperatorCode, string ExpectedValueJson, string ActionCode);
public sealed record PolicyRuleSettingDefinitionResponseDTO(string Code, string Name, string DataTypeCode, bool IsRequired, bool AllowMultiple,
    string? DefaultValueJson, decimal? MinimumValue, decimal? MaximumValue, string? RegexPattern,
    string? Placeholder, string? HelpText, int Order,
    IReadOnlyList<PolicyRuleSettingOptionResponseDTO> Options,
    IReadOnlyList<PolicyRuleSettingDependencyResponseDTO> Dependencies);
public sealed record PolicyRuleDefinitionResponseDTO(string CategoryCode, string RuleTypeCode, string RuleTypeName,
    bool IsRequired, bool AllowMultiple, int Order, IReadOnlyList<PolicyRuleSettingDefinitionResponseDTO> Settings);
public sealed record PolicyTypeResponseDTO(int Id, string Code, string Name, string? Description,
    int? CategoryId, string CategoryCode, string? CurrencyCode, bool IsActive);
public sealed record PolicySummaryResponseDTO(long Id, string Code, string Name, int PolicyTypeId, bool IsActive, long? CurrentVersionId, int? VersionNumber, string? Status);
public sealed record PolicyLeaveTypeResponseDTO(int Id, string Name);
public sealed record PolicyRuleResponseDTO(long Id, int RuleTypeId, string Name, int Order, string Configuration,
    IReadOnlyList<int> LeaveTypeIds);
public sealed record PolicyApplicabilityResponseDTO(long Id, short Mode, int Priority,
    int? CountryId, int? StateId, int? DistrictId, int? LocalityId, long? CampusId,
    int? StaffEmploymentTypeId, int? DepartmentId, int? DesignationId, long? StaffMemberId,
    int? GenderId, short? WorkArrangementType, short? EmploymentStatus,
    int? MinimumServiceDays, DateOnly EffectiveFrom, DateOnly? EffectiveTo,
    IReadOnlyList<int> LeaveTypeIds);
public sealed record PolicyDetailResponseDTO(long Id, string Code, string Name, string? Summary,
    int PolicyTypeId, int? OwnerDepartmentId, string? DefaultCurrencyCode,
    long VersionId, int VersionNumber, short StatusId, string Status,
    DateOnly EffectiveFrom, DateOnly? EffectiveTo, string? ChangeSummary,
    IReadOnlyList<PolicyRuleResponseDTO> Rules,
    IReadOnlyList<PolicyApplicabilityResponseDTO> Applicability,
    AttendancePolicyVersionConfigurationDTO? AttendanceConfiguration,
    IReadOnlyList<PolicyLeaveTypeResponseDTO> LeaveTypes);
public sealed record PolicyAssignmentResultDTO(int Inserted, int Existing);
public sealed record ResolvedPolicyRuleResponseDTO(string RuleTypeCode, string RuleName, int Order, string Configuration,
    IReadOnlyList<int> LeaveTypeIds);
public sealed record ResolvedPolicyExceptionResponseDTO(long Id, short ExceptionType, string OverrideConfiguration, string Reason);
public sealed record ResolvedPolicyResponseDTO(long PolicyId, long PolicyVersionId, string PolicyCode, string PolicyName, int Priority, string ResolutionSource,
    IReadOnlyList<ResolvedPolicyRuleResponseDTO> Rules,
    ResolvedPolicyExceptionResponseDTO? Exception = null);
public sealed record PolicyDocumentResponseDTO(long Id, long PolicyVersionId, short DocumentTypeId, string Title, string OriginalFileName, string ContentType, long FileSizeBytes, string? LanguageCode, bool IsStaffMemberVisible, string Url);
public sealed record PolicyAuditResponseDTO(
    long Id,
    long PolicyId,
    long? PolicyVersionId,
    int? VersionNumber,
    string EntityName,
    long? EntityId,
    string ActionName,
    string? BeforeData,
    string? AfterData,
    long ChangedById,
    string? ChangedByName,
    DateTime ChangedDateTime,
    Guid? CorrelationId);
public sealed record PolicyApprovalStageResponseDTO(long Id, int? PolicyCategoryId, string StageName, int StageOrder, int? ApproverRoleId, int MinimumApprovals, bool IsMandatory, bool IsActive);
public sealed record PolicyApprovalProgressResponseDTO(long StageId, string StageName, int StageOrder, int MinimumApprovals, int ApprovalCount, bool IsComplete);
public sealed record PolicyAssignmentResponseDTO(long Id, long PolicyVersionId, long StaffMemberId,
    short AssignmentSource, DateOnly EffectiveFrom, DateOnly? EffectiveTo, bool IsMandatory,
    bool IsActive, bool IsEffective, string? StateReason);
public sealed record PolicyAssignmentFilterOptionDTO(int Id, string Name, int StaffMemberCount);
public sealed record PolicyAssignmentCandidateDataDTO(long StaffMemberId, string StaffMemberCode, string StaffMemberName,
    int? StaffEmploymentTypeId, string? StaffEmploymentTypeName, int? DepartmentId, string? DepartmentName,
    int? DesignationId, string? DesignationName, bool IsAssigned);
public sealed record PolicyAssignmentCandidateResponseDTO(string StaffMemberId, string StaffMemberCode, string StaffMemberName,
    int? StaffEmploymentTypeId, string? StaffEmploymentTypeName, int? DepartmentId, string? DepartmentName,
    int? DesignationId, string? DesignationName, bool IsAssigned);
public sealed record PolicyAssignmentCandidatePageDTO(long PolicyId, long PolicyVersionId, string PolicyCode,
    string PolicyName, int VersionNumber, DateOnly EffectiveDate, int TotalRecords,
    IReadOnlyList<PolicyAssignmentCandidateResponseDTO> StaffMembers,
    IReadOnlyList<PolicyAssignmentFilterOptionDTO> StaffEmploymentTypes,
    IReadOnlyList<PolicyAssignmentFilterOptionDTO> Departments,
    IReadOnlyList<PolicyAssignmentFilterOptionDTO> Designations);
public sealed record PolicyAssignmentCandidateDataPageDTO(long PolicyId, long PolicyVersionId, string PolicyCode,
    string PolicyName, int VersionNumber, DateOnly EffectiveDate, int TotalRecords,
    IReadOnlyList<PolicyAssignmentCandidateDataDTO> StaffMembers,
    IReadOnlyList<PolicyAssignmentFilterOptionDTO> StaffEmploymentTypes,
    IReadOnlyList<PolicyAssignmentFilterOptionDTO> Departments,
    IReadOnlyList<PolicyAssignmentFilterOptionDTO> Designations);
public sealed record PolicyAssignmentExportRowDTO(long AssignmentId, long StaffMemberId, string StaffMemberCode,
    string StaffMemberName, string? StaffEmploymentTypeName, string? DepartmentName, string? DesignationName,
    short AssignmentSource, DateOnly EffectiveFrom, DateOnly? EffectiveTo, bool IsMandatory, bool IsActive,
    bool IsEffective, string? StateReason);
public sealed record PolicyExceptionResponseDTO(long Id, long PolicyVersionId, long StaffMemberId, short ExceptionType, string OverrideConfiguration, string Reason, DateOnly EffectiveFrom, DateOnly EffectiveTo, short ApprovalStatusId, bool IsActive);
public sealed record PolicyAcknowledgementResponseDTO(long Id, long PolicyVersionId, long StaffMemberId, short Status, DateTime AssignedDateTime, DateTime? ViewedDateTime, DateTime? AcknowledgedDateTime);
