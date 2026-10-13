namespace skolenza.domain.Entity;

public abstract class InstitutionMasterEntity
{
    public int Id { get; set; }
    public long? InstitutionId { get; set; }
    public int? TemplateSourceId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public short DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsSystemDefined { get; set; }
    public long? AddedById { get; set; }
    public DateTime AddedDateTime { get; set; }
    public long? UpdatedById { get; set; }
    public DateTime? UpdatedDateTime { get; set; }
    public bool IsSoftDeleted { get; set; }
    public long? SoftDeletedById { get; set; }
    public DateTime? DeletedDateTime { get; set; }
}

public sealed class StaffJobFamilyMaster : InstitutionMasterEntity;

public sealed class StaffCapabilityMaster : InstitutionMasterEntity
{
    public bool RequiresCertification { get; set; }
    public bool CertificationValidityRequired { get; set; }
}

public sealed class StaffDutyDefinitionMaster : InstitutionMasterEntity
{
    public string? DefaultFrequency { get; set; }
    public bool RequiresEvidence { get; set; }
}

public sealed class StaffPositionMaster : InstitutionMasterEntity
{
    public int StaffJobFamilyMasterId { get; set; }
    public short StaffCategoryId { get; set; }
    public int? DepartmentId { get; set; }
    public int? DesignationId { get; set; }
}

public sealed class StaffPositionCapability
{
    public long Id { get; set; }
    public long? InstitutionId { get; set; }
    public int StaffPositionMasterId { get; set; }
    public int StaffCapabilityMasterId { get; set; }
    public bool IsMandatory { get; set; }
    public short DisplayOrder { get; set; }
    public long? AddedById { get; set; }
    public DateTime AddedDateTime { get; set; }
}

public sealed class StaffPositionDuty
{
    public long Id { get; set; }
    public long? InstitutionId { get; set; }
    public int StaffPositionMasterId { get; set; }
    public int StaffDutyDefinitionMasterId { get; set; }
    public bool IsMandatory { get; set; }
    public short DisplayOrder { get; set; }
    public long? AddedById { get; set; }
    public DateTime AddedDateTime { get; set; }
}

public sealed class StaffPositionAssignment
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long StaffMemberId { get; set; }
    public int StaffPositionMasterId { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; }
    public string? Remark { get; set; }
    public long AddedById { get; set; }
    public DateTime AddedDateTime { get; set; }
    public long? UpdatedById { get; set; }
    public DateTime? UpdatedDateTime { get; set; }
}

public sealed class StaffCapabilityAssignment
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long StaffMemberId { get; set; }
    public int StaffCapabilityMasterId { get; set; }
    public long? SourcePositionAssignmentId { get; set; }
    public short VerificationStatus { get; set; }
    public bool IsRequired { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public long? VerifiedById { get; set; }
    public DateTime? VerifiedDateTime { get; set; }
    public string? VerificationRemark { get; set; }
    public long AddedById { get; set; }
    public DateTime AddedDateTime { get; set; }
    public long? UpdatedById { get; set; }
    public DateTime? UpdatedDateTime { get; set; }
}

public sealed class StaffDutyAssignment
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long StaffMemberId { get; set; }
    public int StaffDutyDefinitionMasterId { get; set; }
    public long? SourcePositionAssignmentId { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; }
    public string? Remark { get; set; }
    public long AddedById { get; set; }
    public DateTime AddedDateTime { get; set; }
    public long? UpdatedById { get; set; }
    public DateTime? UpdatedDateTime { get; set; }
}

public abstract class InstitutionAcademicEntity
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsSoftDeleted { get; set; }
    public long AddedById { get; set; }
    public DateTime AddedDateTime { get; set; }
    public long? UpdatedById { get; set; }
    public DateTime? UpdatedDateTime { get; set; }
}

public sealed class AcademicSession : InstitutionAcademicEntity
{
    public long? CampusId { get; set; }
    public long? ParentAcademicSessionId { get; set; }
    public short SessionType { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
}

public sealed class AcademicProgramme : InstitutionAcademicEntity
{
    public string? Level { get; set; }
}

public sealed class AcademicGroup : InstitutionAcademicEntity
{
    public long AcademicSessionId { get; set; }
    public long? AcademicProgrammeId { get; set; }
    public long? ParentAcademicGroupId { get; set; }
    public short GroupType { get; set; }
}

public sealed class ClassSection : InstitutionAcademicEntity
{
    public long AcademicGroupId { get; set; }
    public long? CampusId { get; set; }
    public int? Capacity { get; set; }
}

public sealed class Subject : InstitutionAcademicEntity
{
    public short SubjectType { get; set; }
}

public sealed class CourseOffering : InstitutionAcademicEntity
{
    public long AcademicSessionId { get; set; }
    public long AcademicGroupId { get; set; }
    public long SubjectId { get; set; }
    public string? CurriculumVersion { get; set; }
    public short? RequiredPeriodsPerWeek { get; set; }
}

/// <summary>Central assignment-role catalogue shared by all institutions.</summary>
public sealed class TeachingAssignmentTypeMaster
{
    public short Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public short DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class StaffTeachingAssignment
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long StaffMemberId { get; set; }
    public long CourseOfferingId { get; set; }
    public long ClassSectionId { get; set; }
    public short AssignmentType { get; set; }
    public decimal ContributionPercent { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; } = true;
    public long AddedById { get; set; }
    public DateTime AddedDateTime { get; set; }
    public long? UpdatedById { get; set; }
    public DateTime? UpdatedDateTime { get; set; }
}

public sealed class ClassTeacherAssignment
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long StaffMemberId { get; set; }
    public long ClassSectionId { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsPrimary { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public long AddedById { get; set; }
    public DateTime AddedDateTime { get; set; }
    public long? UpdatedById { get; set; }
    public DateTime? UpdatedDateTime { get; set; }
}

/// <summary>Central workflow-state catalogue used by curriculum planning and delivery review.</summary>
public sealed class AcademicWorkflowStatusMaster
{
    public short Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public short DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class CurriculumFrameworkMaster : InstitutionAcademicEntity
{
    public long AcademicSessionId { get; set; }
    public string AuthorityName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public short WorkflowStatusId { get; set; }
    public long? ReviewedById { get; set; }
    public DateTime? ReviewedDateTime { get; set; }
    public string? ReviewRemark { get; set; }
}

public sealed class CurriculumUnit : InstitutionAcademicEntity
{
    public long CurriculumFrameworkId { get; set; }
    public long CourseOfferingId { get; set; }
    public long? ParentCurriculumUnitId { get; set; }
    public short Sequence { get; set; }
    public short PlannedPeriods { get; set; }
    public string? LearningOutcome { get; set; }
}

public sealed class CurriculumTopic : InstitutionAcademicEntity
{
    public long CurriculumUnitId { get; set; }
    public short Sequence { get; set; }
    public short PlannedPeriods { get; set; }
    public string? LearningOutcome { get; set; }
}

public sealed class LessonPlan
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long StaffTeachingAssignmentId { get; set; }
    public DateOnly PlannedDate { get; set; }
    public string? PeriodReference { get; set; }
    public string Objectives { get; set; } = string.Empty;
    public string? TeachingMethod { get; set; }
    public string? Resources { get; set; }
    public string? Activities { get; set; }
    public string? Assessment { get; set; }
    public string? Homework { get; set; }
    public short WorkflowStatusId { get; set; }
    public long? ReviewedById { get; set; }
    public DateTime? ReviewedDateTime { get; set; }
    public string? ReviewRemark { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsSoftDeleted { get; set; }
    public long AddedById { get; set; }
    public DateTime AddedDateTime { get; set; }
    public long? UpdatedById { get; set; }
    public DateTime? UpdatedDateTime { get; set; }
}

public sealed class LessonPlanCurriculumTopic
{
    public long LessonPlanId { get; set; }
    public long CurriculumTopicId { get; set; }
}

public sealed class LessonDeliveryLog
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long LessonPlanId { get; set; }
    public DateOnly DeliveredDate { get; set; }
    public string CoverageNotes { get; set; } = string.Empty;
    public string? Homework { get; set; }
    public string? EvidenceReference { get; set; }
    public string? ExceptionReason { get; set; }
    public string? ReplanAction { get; set; }
    public short WorkflowStatusId { get; set; }
    public long? ReviewedById { get; set; }
    public DateTime? ReviewedDateTime { get; set; }
    public string? ReviewRemark { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsSoftDeleted { get; set; }
    public long AddedById { get; set; }
    public DateTime AddedDateTime { get; set; }
    public long? UpdatedById { get; set; }
    public DateTime? UpdatedDateTime { get; set; }
}

public sealed class LessonDeliveryCoverage
{
    public long Id { get; set; }
    public long LessonDeliveryLogId { get; set; }
    public long CurriculumTopicId { get; set; }
    public decimal CompletionPercent { get; set; }
}
