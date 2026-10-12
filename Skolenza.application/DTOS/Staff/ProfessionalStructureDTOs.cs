using skolenza.application.DTOs.BaseDTO;

namespace skolenza.application.DTOS.Staff;

public enum StaffProfessionalMasterType
{
    JobFamily = 1,
    Capability = 2,
    DutyDefinition = 3
}

public sealed class StaffProfessionalPermissionRequestDTO : PermissionRequestDTO;

public sealed class StaffProfessionalDeleteRequestDTO : PermissionRequestDTO
{
    public int Id { get; set; }
}

public sealed class StaffProfessionalProfileRequestDTO : PermissionRequestDTO
{
    public string StaffMemberId { get; set; } = string.Empty;
}

public sealed class StaffProfessionalMasterRequestDTO : PermissionRequestDTO
{
    public int? Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public short DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public bool RequiresCertification { get; set; }
    public bool CertificationValidityRequired { get; set; }
    public string? DefaultFrequency { get; set; }
    public bool RequiresEvidence { get; set; }
}

public sealed class StaffProfessionalMasterResponseDTO
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public short DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public bool RequiresCertification { get; set; }
    public bool CertificationValidityRequired { get; set; }
    public string? DefaultFrequency { get; set; }
    public bool RequiresEvidence { get; set; }
}

public sealed class StaffPositionMasterRequestDTO : PermissionRequestDTO
{
    public int? Id { get; set; }
    public int StaffJobFamilyMasterId { get; set; }
    public short StaffCategoryId { get; set; }
    public int? DepartmentId { get; set; }
    public int? DesignationId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public short DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public List<int> CapabilityIds { get; set; } = [];
    public List<int> DutyDefinitionIds { get; set; } = [];
}

public sealed class StaffPositionMasterResponseDTO
{
    public int Id { get; set; }
    public int StaffJobFamilyMasterId { get; set; }
    public string JobFamilyName { get; set; } = string.Empty;
    public short StaffCategoryId { get; set; }
    public string StaffCategoryName { get; set; } = string.Empty;
    public int? DepartmentId { get; set; }
    public int? DesignationId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public short DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public List<StaffProfessionalMasterResponseDTO> Capabilities { get; set; } = [];
    public List<StaffProfessionalMasterResponseDTO> Duties { get; set; } = [];
}

public sealed class StaffProfessionalProfileResponseDTO
{
    public long? PositionAssignmentId { get; set; }
    public int? StaffPositionMasterId { get; set; }
    public string? PositionName { get; set; }
    public int? StaffJobFamilyMasterId { get; set; }
    public string? JobFamilyName { get; set; }
    public DateOnly? EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsPrimary { get; set; }
    public List<StaffCapabilityAssignmentResponseDTO> Capabilities { get; set; } = [];
    public List<StaffDutyAssignmentResponseDTO> Duties { get; set; } = [];
}

public sealed class StaffCapabilityAssignmentResponseDTO
{
    public long Id { get; set; }
    public int StaffCapabilityMasterId { get; set; }
    public string Name { get; set; } = string.Empty;
    public short VerificationStatus { get; set; }
    public string VerificationStatusName { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public DateTime? VerifiedDateTime { get; set; }
    public string? VerificationRemark { get; set; }
}

public sealed class StaffDutyAssignmentResponseDTO
{
    public long Id { get; set; }
    public int StaffDutyDefinitionMasterId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Frequency { get; set; }
    public bool IsActive { get; set; }
}

public sealed class VerifyStaffCapabilityRequestDTO : PermissionRequestDTO
{
    public long AssignmentId { get; set; }
    public bool IsApproved { get; set; }
    public string? Remark { get; set; }
}
