using skolenza.application.DTOs.BaseDTO;

namespace skolenza.application.DTOS.Staff;

public enum AcademicMasterType
{
    Session = 1,
    Programme = 2,
    Group = 3,
    Section = 4,
    Subject = 5,
    CourseOffering = 6
}

public sealed class AcademicStructurePermissionRequestDTO : PermissionRequestDTO;

public sealed class TeachingAssignmentTypeOptionDTO
{
    public short Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public sealed class AcademicMasterDeleteRequestDTO : PermissionRequestDTO
{
    public long Id { get; set; }
}

public sealed class AcademicMasterRequestDTO : PermissionRequestDTO
{
    public long? Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public long? CampusId { get; set; }
    public long? ParentId { get; set; }
    public long? AcademicSessionId { get; set; }
    public long? AcademicProgrammeId { get; set; }
    public long? AcademicGroupId { get; set; }
    public long? SubjectId { get; set; }
    public short Type { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? Level { get; set; }
    public int? Capacity { get; set; }
    public string? CurriculumVersion { get; set; }
    public short? RequiredPeriodsPerWeek { get; set; }
}

public sealed class AcademicMasterResponseDTO
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public long? CampusId { get; set; }
    public long? ParentId { get; set; }
    public long? AcademicSessionId { get; set; }
    public string? AcademicSessionName { get; set; }
    public long? AcademicProgrammeId { get; set; }
    public string? AcademicProgrammeName { get; set; }
    public long? AcademicGroupId { get; set; }
    public string? AcademicGroupName { get; set; }
    public long? SubjectId { get; set; }
    public string? SubjectName { get; set; }
    public short Type { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? Level { get; set; }
    public int? Capacity { get; set; }
    public string? CurriculumVersion { get; set; }
    public short? RequiredPeriodsPerWeek { get; set; }
}

public sealed class StaffAcademicProfileRequestDTO : PermissionRequestDTO
{
    public string StaffMemberId { get; set; } = string.Empty;
}

public sealed class StaffTeachingAssignmentRequestDTO : PermissionRequestDTO
{
    public long? Id { get; set; }
    public string StaffMemberId { get; set; } = string.Empty;
    public long CourseOfferingId { get; set; }
    public long ClassSectionId { get; set; }
    public short AssignmentType { get; set; }
    public decimal ContributionPercent { get; set; } = 100;
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class ClassTeacherAssignmentRequestDTO : PermissionRequestDTO
{
    public long? Id { get; set; }
    public string StaffMemberId { get; set; } = string.Empty;
    public long ClassSectionId { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsPrimary { get; set; } = true;
    public bool IsActive { get; set; } = true;
}

public sealed class StaffTeachingAssignmentResponseDTO
{
    public long Id { get; set; }
    public long CourseOfferingId { get; set; }
    public string CourseOfferingName { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public long ClassSectionId { get; set; }
    public string ClassSectionName { get; set; } = string.Empty;
    public string AcademicGroupName { get; set; } = string.Empty;
    public short AssignmentType { get; set; }
    public string AssignmentTypeName { get; set; } = string.Empty;
    public decimal ContributionPercent { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; }
}

public sealed class ClassTeacherAssignmentResponseDTO
{
    public long Id { get; set; }
    public long ClassSectionId { get; set; }
    public string ClassSectionName { get; set; } = string.Empty;
    public string AcademicGroupName { get; set; } = string.Empty;
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; }
}

public sealed class StaffAcademicProfileResponseDTO
{
    public List<StaffTeachingAssignmentResponseDTO> TeachingAssignments { get; set; } = [];
    public List<ClassTeacherAssignmentResponseDTO> ClassTeacherAssignments { get; set; } = [];
}
