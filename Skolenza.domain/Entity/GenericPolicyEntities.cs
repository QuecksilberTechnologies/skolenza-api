namespace skolenza.domain.Entity;

public sealed class PolicyCategory
{
    public int Id { get; set; }
    public string CategoryCode { get; set; } = null!;
    public string CategoryName { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public long? AddedById { get; set; }
    public DateTime AddedDateTime { get; set; }
    public long? UpdatedById { get; set; }
    public DateTime? UpdatedDateTime { get; set; }
}

public sealed class PolicyStatus
{
    public short Id { get; set; }
    public string StatusCode { get; set; } = null!;
    public string StatusName { get; set; } = null!;
    public bool IsTerminal { get; set; }
    public bool IsActive { get; set; }
}

public sealed class PolicyRuleType
{
    public int Id { get; set; }
    public string RuleTypeCode { get; set; } = null!;
    public string RuleTypeName { get; set; } = null!;
    public string? Description { get; set; }
    public int JsonSchemaVersion { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>Declares which rule types a policy category supports.</summary>
public sealed class PolicyCategoryRuleType
{
    public int Id { get; set; }
    public int PolicyCategoryId { get; set; }
    public int PolicyRuleTypeId { get; set; }
    public bool IsRequired { get; set; }
    public bool AllowMultiple { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>Defines one validated, UI-renderable setting for a policy rule type.</summary>
public sealed class PolicyRuleSettingDefinition
{
    public int Id { get; set; }
    public int PolicyRuleTypeId { get; set; }
    public string SettingCode { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string DataTypeCode { get; set; } = null!;
    public bool IsRequired { get; set; }
    public bool AllowMultiple { get; set; }
    public string? DefaultValueJson { get; set; }
    public decimal? MinimumValue { get; set; }
    public decimal? MaximumValue { get; set; }
    public string? RegexPattern { get; set; }
    public string? Placeholder { get; set; }
    public string? HelpText { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>Defines a stable code/value option for select-style rule settings.</summary>
public sealed class PolicyRuleSettingOption
{
    public int Id { get; set; }
    public int PolicyRuleSettingDefinitionId { get; set; }
    public string OptionCode { get; set; } = null!;
    public string OptionLabel { get; set; } = null!;
    public string ValueJson { get; set; } = null!;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>Controls conditional visibility or required state between rule settings.</summary>
public sealed class PolicyRuleSettingDependency
{
    public int Id { get; set; }
    public int PolicyRuleSettingDefinitionId { get; set; }
    public int DependsOnSettingDefinitionId { get; set; }
    public string OperatorCode { get; set; } = null!;
    public string ExpectedValueJson { get; set; } = null!;
    public string ActionCode { get; set; } = null!;
}

public sealed class PolicyDocumentType
{
    public short Id { get; set; }
    public string DocumentTypeCode { get; set; } = null!;
    public string DocumentTypeName { get; set; } = null!;
    public bool IsActive { get; set; }
}

public sealed class Policy
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public int PolicyTypeId { get; set; }
    public string PolicyCode { get; set; } = null!;
    public string PolicyName { get; set; } = null!;
    public string? Summary { get; set; }
    public int? OwnerDepartmentId { get; set; }
    public string? DefaultCurrencyCode { get; set; }
    public bool IsActive { get; set; }
    public bool IsSoftDeleted { get; set; }
    public long AddedById { get; set; }
    public DateTime AddedDateTime { get; set; }
    public long? UpdatedById { get; set; }
    public DateTime? UpdatedDateTime { get; set; }
    public long? SoftDeletedById { get; set; }
    public DateTime? SoftDeletedDateTime { get; set; }
    public ICollection<PolicyVersion> PolicyVersions { get; set; } = new List<PolicyVersion>();
}

public sealed class PolicyVersion
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long PolicyId { get; set; }
    public int VersionNumber { get; set; }
    public short PolicyStatusId { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public string? ChangeSummary { get; set; }
    public int RuleSchemaVersion { get; set; }
    public long? ApprovedById { get; set; }
    public DateTime? ApprovedDateTime { get; set; }
    public string? ApprovedContentChecksumSha256 { get; set; }
    public long? PublishedById { get; set; }
    public DateTime? PublishedDateTime { get; set; }
    public bool IsCurrent { get; set; }
    public bool IsActive { get; set; }
    public long AddedById { get; set; }
    public DateTime AddedDateTime { get; set; }
    public long? UpdatedById { get; set; }
    public DateTime? UpdatedDateTime { get; set; }
    public Policy Policy { get; set; } = null!;
    public AttendancePolicyVersionConfiguration? AttendanceConfiguration { get; set; }
    public ICollection<StaffMemberWorkArrangement> StaffMemberWorkArrangements { get; set; } = new List<StaffMemberWorkArrangement>();
}

/// <summary>Stores the typed execution settings owned by one generic Attendance policy version.</summary>
public sealed class AttendancePolicyVersionConfiguration
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long PolicyVersionId { get; set; }
    public short AttendanceLocationScope { get; set; }
    public bool AllowBiometric { get; set; }
    public bool AllowMobile { get; set; }
    public bool AllowWeb { get; set; }
    public bool AllowManualAttendance { get; set; }
    public bool AllowWorkFromHome { get; set; }
    public bool RequireGeoFenceForOffice { get; set; }
    public bool RequireGpsForRemote { get; set; }
    public bool AllowOutsideLocationWithApproval { get; set; }
    public long AddedById { get; set; }
    public DateTime AddedDateTime { get; set; }
    public long? UpdatedById { get; set; }
    public DateTime? UpdatedDateTime { get; set; }
    public PolicyVersion PolicyVersion { get; set; } = null!;
}

public sealed class PolicyRule
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long PolicyVersionId { get; set; }
    public int PolicyRuleTypeId { get; set; }
    public string RuleName { get; set; } = null!;
    public int RuleOrder { get; set; }
    public string RuleConfiguration { get; set; } = "{}";
    public bool IsActive { get; set; }
    public long AddedById { get; set; }
    public DateTime AddedDateTime { get; set; }
    public long? UpdatedById { get; set; }
    public DateTime? UpdatedDateTime { get; set; }
}

/// <summary>Declares a leave component covered by one immutable policy version.</summary>
public sealed class PolicyVersionLeaveType
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long PolicyVersionId { get; set; }
    public int LeaveTypeId { get; set; }
    public bool IsActive { get; set; }
    public long AddedById { get; set; }
    public DateTime AddedDateTime { get; set; }
}

/// <summary>Targets one policy rule at one covered leave component.</summary>
public sealed class PolicyRuleLeaveType
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long PolicyRuleId { get; set; }
    public int LeaveTypeId { get; set; }
}

/// <summary>Targets one applicability condition at one covered leave component.</summary>
public sealed class PolicyApplicabilityLeaveType
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long PolicyApplicabilityId { get; set; }
    public int LeaveTypeId { get; set; }
}

public sealed class PolicyApplicability
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long PolicyVersionId { get; set; }
    public short ApplicabilityMode { get; set; }
    public int? CountryId { get; set; }
    public int? StateId { get; set; }
    public int? DistrictId { get; set; }
    public int? LocalityId { get; set; }
    public long? CampusId { get; set; }
    public int? StaffEmploymentTypeId { get; set; }
    public int? DepartmentId { get; set; }
    public int? StaffPositionMasterId { get; set; }
    public long? StaffMemberId { get; set; }
    public int? GenderId { get; set; }
    public short? WorkArrangementType { get; set; }
    public short? EmploymentStatus { get; set; }
    public int? MinimumServiceDays { get; set; }
    public int Priority { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; }
    public long AddedById { get; set; }
    public DateTime AddedDateTime { get; set; }
    public long? UpdatedById { get; set; }
    public DateTime? UpdatedDateTime { get; set; }
}

public sealed class PolicyAssignment
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long PolicyVersionId { get; set; }
    public long StaffMemberId { get; set; }
    public short AssignmentSource { get; set; }
    public long? SourceApplicabilityId { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsMandatory { get; set; }
    public bool IsActive { get; set; }
    public long AssignedById { get; set; }
    public DateTime AssignedDateTime { get; set; }
    public long? RemovedById { get; set; }
    public DateTime? RemovedDateTime { get; set; }
}

public sealed class PolicyException
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long PolicyVersionId { get; set; }
    public long StaffMemberId { get; set; }
    public short ExceptionType { get; set; }
    public string OverrideConfiguration { get; set; } = "{}";
    public string Reason { get; set; } = null!;
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly EffectiveTo { get; set; }
    public short ApprovalStatusId { get; set; }
    public long? ApprovedById { get; set; }
    public DateTime? ApprovedDateTime { get; set; }
    public bool IsActive { get; set; }
    public long AddedById { get; set; }
    public DateTime AddedDateTime { get; set; }
}

public sealed class PolicyDocument
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long PolicyVersionId { get; set; }
    public short PolicyDocumentTypeId { get; set; }
    public string DocumentTitle { get; set; } = null!;
    public string OriginalFileName { get; set; } = null!;
    public string StorageProvider { get; set; } = null!;
    public string ObjectKey { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public long FileSizeBytes { get; set; }
    public string ChecksumSha256 { get; set; } = null!;
    public string? LanguageCode { get; set; }
    public bool IsStaffMemberVisible { get; set; }
    public bool IsActive { get; set; }
    public bool IsSoftDeleted { get; set; }
    public long AddedById { get; set; }
    public DateTime AddedDateTime { get; set; }
    public long? SoftDeletedById { get; set; }
    public DateTime? SoftDeletedDateTime { get; set; }
}

public sealed class PolicyApprovalStage
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public int? PolicyCategoryId { get; set; }
    public string StageName { get; set; } = null!;
    public int StageOrder { get; set; }
    public int? ApproverRoleId { get; set; }
    public int MinimumApprovals { get; set; }
    public bool IsMandatory { get; set; }
    public bool IsActive { get; set; }
    public long AddedById { get; set; }
    public DateTime AddedDateTime { get; set; }
}

public sealed class PolicyApprovalHistory
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long PolicyVersionId { get; set; }
    public long PolicyApprovalStageId { get; set; }
    public short ActionType { get; set; }
    public long ActionById { get; set; }
    public DateTime ActionDateTime { get; set; }
    public string? Comments { get; set; }
    public int SequenceNumber { get; set; }
    public string ContentChecksumSha256 { get; set; } = null!;
}

public sealed class PolicyAcknowledgement
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long PolicyVersionId { get; set; }
    public long StaffMemberId { get; set; }
    public short AcknowledgementStatus { get; set; }
    public DateTime AssignedDateTime { get; set; }
    public DateTime? ViewedDateTime { get; set; }
    public DateTime? AcknowledgedDateTime { get; set; }
    public string? SourceIpHash { get; set; }
    public string? EvidenceJson { get; set; }
}

public sealed class PolicyChangeAudit
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long PolicyId { get; set; }
    public long? PolicyVersionId { get; set; }
    public string EntityName { get; set; } = null!;
    public long? EntityId { get; set; }
    public string ActionName { get; set; } = null!;
    public string? BeforeData { get; set; }
    public string? AfterData { get; set; }
    public long ChangedById { get; set; }
    public DateTime ChangedDateTime { get; set; }
    public Guid? CorrelationId { get; set; }
}
