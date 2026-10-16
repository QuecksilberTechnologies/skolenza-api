using skolenza.application.DTOs.BaseDTO;

namespace skolenza.application.DTOS.Staff;

/// <summary>Canonical effective primary Position used by staff-facing read contracts.</summary>
public sealed class StaffPositionIdentityReadModel
{
    public long InstitutionId { get; set; }
    public long StaffMemberId { get; set; }
    public int StaffPositionMasterId { get; set; }
    public string PositionName { get; set; } = string.Empty;
}

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

public sealed class InstitutionStaffCategoryRequestDTO : PermissionRequestDTO
{
    public int? Id { get; set; }
    public short StaffCategoryTypeId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public short DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class InstitutionStaffCategoryResponseDTO
{
    public int Id { get; set; }
    public short StaffCategoryTypeId { get; set; }
    public string StaffCategoryTypeName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public short DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public int ActiveJobFamilyCount { get; set; }
    public List<int> StaffJobFamilyMasterIds { get; set; } = [];
    public int ActivePositionCount { get; set; }
    public int ActiveStaffCount { get; set; }
}

public sealed class StaffCategoryJobFamilyRequestDTO : PermissionRequestDTO
{
    public int InstitutionStaffCategoryMasterId { get; set; }
    public List<int> StaffJobFamilyMasterIds { get; set; } = [];
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

public class StaffProfessionalMasterResponseDTO
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
    public int InstitutionStaffCategoryMasterId { get; set; }
    public int? DepartmentId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public short DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public List<StaffPositionRequirementRequestDTO> Capabilities { get; set; } = [];
    public List<StaffPositionRequirementRequestDTO> Duties { get; set; } = [];
}

public sealed class StaffPositionRequirementRequestDTO
{
    public int Id { get; set; }
    public bool IsMandatory { get; set; } = true;
    public short DisplayOrder { get; set; }
}

public sealed class StaffPositionRequirementResponseDTO : StaffProfessionalMasterResponseDTO
{
    public bool IsMandatory { get; set; }
}

public sealed class StaffPositionMasterResponseDTO
{
    public int Id { get; set; }
    public int StaffJobFamilyMasterId { get; set; }
    public int? InstitutionStaffCategoryMasterId { get; set; }
    public string JobFamilyName { get; set; } = string.Empty;
    public short StaffCategoryId { get; set; }
    public string StaffCategoryName { get; set; } = string.Empty;
    public int? DepartmentId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public short DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public List<StaffPositionRequirementResponseDTO> Capabilities { get; set; } = [];
    public List<StaffPositionRequirementResponseDTO> Duties { get; set; } = [];
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
