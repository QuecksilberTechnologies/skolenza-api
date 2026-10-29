using System.ComponentModel.DataAnnotations;
using skolenza.application.Common.Enums;
using skolenza.application.DTOs.BaseDTO;
using skolenza.application.DTOS.Pagination;

namespace skolenza.application.DTOS.Student;

public sealed class StudentGuardianFilterDTO : PermissionPagedRequestDTO
{
    public StudentGuardianFilterDTO()
    {
        PageNumber = PaginationDefaults.PageNumber;
        PageSize = PaginationDefaults.PageSize;
    }

    public bool ActiveOnly { get; set; } = true;
    public string? Search { get; set; }
}

public sealed class StudentGuardianProfileRequestDTO : PermissionRequestDTO
{
    [Required]
    public string FirstName { get; set; } = string.Empty;

    public string? MiddleName { get; set; }

    [Required]
    public string LastName { get; set; } = string.Empty;

    [EmailAddress]
    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }
}

public sealed class StudentGuardianLinkRequestDTO : PermissionRequestDTO
{
    public long GuardianId { get; set; }
    public StudentGuardianRelationshipType RelationshipType { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsEmergencyContact { get; set; }
    public bool IsPickupAuthorized { get; set; }
    public bool HasPortalAccess { get; set; } = true;
    public DateOnly EffectiveFrom { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateOnly? EffectiveTo { get; set; }
}

public sealed class StudentGuardianLinksFilterDTO : PermissionRequestDTO
{
    public bool IncludeInactive { get; set; }
}

public sealed class StudentGuardianUnlinkRequestDTO : PermissionRequestDTO
{
    public DateOnly EffectiveTo { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
}

public sealed class StudentAdmissionGuardianRequestDTO : PermissionRequestDTO
{
    public long GuardianId { get; set; }
    public StudentGuardianRelationshipType RelationshipType { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsEmergencyContact { get; set; }
    public bool IsPickupAuthorized { get; set; }
    public bool HasPortalAccess { get; set; } = true;
    public DateOnly EffectiveFrom { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateOnly? EffectiveTo { get; set; }
}

public sealed class StudentGuardianProfileResponseDTO
{
    public long Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
}

public sealed class StudentGuardianLinkResponseDTO
{
    public long Id { get; set; }
    public long StudentId { get; set; }
    public StudentGuardianProfileResponseDTO Guardian { get; set; } = new();
    public StudentGuardianRelationshipType RelationshipType { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsEmergencyContact { get; set; }
    public bool IsPickupAuthorized { get; set; }
    public bool HasPortalAccess { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; }
}

public sealed class StudentAdmissionGuardianResponseDTO
{
    public long Id { get; set; }
    public long StudentAdmissionId { get; set; }
    public StudentGuardianProfileResponseDTO Guardian { get; set; } = new();
    public StudentGuardianRelationshipType RelationshipType { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsEmergencyContact { get; set; }
    public bool IsPickupAuthorized { get; set; }
    public bool HasPortalAccess { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; }
}
