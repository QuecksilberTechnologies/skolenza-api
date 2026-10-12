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
