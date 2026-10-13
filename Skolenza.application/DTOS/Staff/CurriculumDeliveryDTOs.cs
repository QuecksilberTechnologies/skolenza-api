using skolenza.application.DTOs.BaseDTO;

namespace skolenza.application.DTOS.Staff;

public sealed class AcademicWorkflowStatusOptionDTO
{
    public short Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public sealed class CurriculumDeliveryPermissionRequestDTO : PermissionRequestDTO;

public sealed class CurriculumFrameworkRequestDTO : PermissionRequestDTO
{
    public long? Id { get; set; }
    public long AcademicSessionId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string AuthorityName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class CurriculumFrameworkResponseDTO
{
    public long Id { get; set; }
    public long AcademicSessionId { get; set; }
    public string AcademicSessionName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string AuthorityName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public short WorkflowStatusId { get; set; }
    public string WorkflowStatusName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class CurriculumUnitRequestDTO : PermissionRequestDTO
{
    public long? Id { get; set; }
    public long CurriculumFrameworkId { get; set; }
    public long CourseOfferingId { get; set; }
    public long? ParentCurriculumUnitId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public short Sequence { get; set; }
    public short PlannedPeriods { get; set; }
    public string? LearningOutcome { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class CurriculumTopicRequestDTO : PermissionRequestDTO
{
    public long? Id { get; set; }
    public long CurriculumUnitId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public short Sequence { get; set; }
    public short PlannedPeriods { get; set; }
    public string? LearningOutcome { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class CurriculumUnitResponseDTO
{
    public long Id { get; set; }
    public long CurriculumFrameworkId { get; set; }
    public long CourseOfferingId { get; set; }
    public string CourseOfferingName { get; set; } = string.Empty;
    public long? ParentCurriculumUnitId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public short Sequence { get; set; }
    public short PlannedPeriods { get; set; }
    public string? LearningOutcome { get; set; }
    public bool IsActive { get; set; }
    public List<CurriculumTopicResponseDTO> Topics { get; set; } = [];
}

public sealed class CurriculumTopicResponseDTO
{
    public long Id { get; set; }
    public long CurriculumUnitId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public short Sequence { get; set; }
    public short PlannedPeriods { get; set; }
    public string? LearningOutcome { get; set; }
    public bool IsActive { get; set; }
}

public sealed class LessonPlanRequestDTO : PermissionRequestDTO
{
    public long? Id { get; set; }
    public long StaffTeachingAssignmentId { get; set; }
    public DateOnly PlannedDate { get; set; }
    public string? PeriodReference { get; set; }
    public string Objectives { get; set; } = string.Empty;
    public string? TeachingMethod { get; set; }
    public string? Resources { get; set; }
    public string? Activities { get; set; }
    public string? Assessment { get; set; }
    public string? Homework { get; set; }
    public bool IsActive { get; set; } = true;
    public List<long> CurriculumTopicIds { get; set; } = [];
}

public sealed class LessonPlanResponseDTO
{
    public long Id { get; set; }
    public long StaffTeachingAssignmentId { get; set; }
    public string StaffMemberName { get; set; } = string.Empty;
    public string CourseOfferingName { get; set; } = string.Empty;
    public DateOnly PlannedDate { get; set; }
    public string? PeriodReference { get; set; }
    public string Objectives { get; set; } = string.Empty;
    public string? TeachingMethod { get; set; }
    public string? Resources { get; set; }
    public string? Activities { get; set; }
    public string? Assessment { get; set; }
    public string? Homework { get; set; }
    public short WorkflowStatusId { get; set; }
    public string WorkflowStatusName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public List<CurriculumTopicResponseDTO> Topics { get; set; } = [];
}

public sealed class WorkflowReviewRequestDTO : PermissionRequestDTO
{
    public long Id { get; set; }
    public short WorkflowStatusId { get; set; }
    public string? Remark { get; set; }
}

public sealed class LessonDeliveryRequestDTO : PermissionRequestDTO
{
    public long? Id { get; set; }
    public long LessonPlanId { get; set; }
    public DateOnly DeliveredDate { get; set; }
    public string CoverageNotes { get; set; } = string.Empty;
    public string? Homework { get; set; }
    public string? EvidenceReference { get; set; }
    public string? ExceptionReason { get; set; }
    public string? ReplanAction { get; set; }
    public bool IsActive { get; set; } = true;
    public List<LessonDeliveryCoverageRequestDTO> Coverage { get; set; } = [];
}

public sealed class LessonDeliveryCoverageRequestDTO
{
    public long CurriculumTopicId { get; set; }
    public decimal CompletionPercent { get; set; }
}

public sealed class LessonDeliveryResponseDTO
{
    public long Id { get; set; }
    public long LessonPlanId { get; set; }
    public DateOnly DeliveredDate { get; set; }
    public string CoverageNotes { get; set; } = string.Empty;
    public string? Homework { get; set; }
    public string? EvidenceReference { get; set; }
    public string? ExceptionReason { get; set; }
    public string? ReplanAction { get; set; }
    public short WorkflowStatusId { get; set; }
    public string WorkflowStatusName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public List<LessonDeliveryCoverageRequestDTO> Coverage { get; set; } = [];
}

public sealed class SyllabusProgressResponseDTO
{
    public long CurriculumFrameworkId { get; set; }
    public string CurriculumFrameworkName { get; set; } = string.Empty;
    public long CourseOfferingId { get; set; }
    public string CourseOfferingName { get; set; } = string.Empty;
    public long CurriculumUnitId { get; set; }
    public string CurriculumUnitName { get; set; } = string.Empty;
    public int TopicCount { get; set; }
    public decimal CompletionPercent { get; set; }
}
