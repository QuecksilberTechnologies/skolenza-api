using skolenza.application.Common.Enums;
using skolenza.application.DTOs.BaseDTO;
using skolenza.application.DTOS.Pagination;

namespace skolenza.application.DTOS.Student;

public sealed class InstitutionPortalDomainFilterDTO : PermissionPagedRequestDTO
{
    public InstitutionPortalDomainFilterDTO()
    {
        PageNumber = PaginationDefaults.PageNumber;
        PageSize = PaginationDefaults.PageSize;
    }

    public bool ActiveOnly { get; set; }
}

public sealed class InstitutionPortalDomainCreateDTO : PermissionRequestDTO
{
    public string DomainName { get; set; } = string.Empty;
}

public sealed class InstitutionPortalDomainResponseDTO
{
    public long Id { get; set; }
    public string DomainName { get; set; } = string.Empty;
    public StudentPortalDomainVerificationStatus VerificationStatus { get; set; }
    public DateTime ChallengeExpiresAt { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public bool IsActive { get; set; }
    public DateTime AddedDateTime { get; set; }
    public DateTime? UpdatedDateTime { get; set; }
}

/// <summary>Contains a one-time TXT challenge; do not cache or log this response.</summary>
public sealed class InstitutionPortalDomainChallengeResponseDTO
{
    public InstitutionPortalDomainResponseDTO Domain { get; set; } = new();
    public string DnsTxtRecordName { get; set; } = string.Empty;
    public string DnsTxtRecordValue { get; set; } = string.Empty;
}

public sealed class InstitutionPortalDomainVerifyDTO : PermissionRequestDTO
{
}

public sealed class InstitutionPortalDomainDeleteDTO : PermissionRequestDTO
{
}
