using skolenza.application.Common.Enums;
using skolenza.application.DTOS.Pagination;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace skolenza.application.DTOS.Student;

public sealed class StudentPortalLoginRequestDTO
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class StudentPortalRefreshRequestDTO
{
    public string RefreshToken { get; set; } = string.Empty;
}

public sealed class StudentPortalInvitationAcceptanceRequestDTO
{
    public string Token { get; set; } = string.Empty;
    public string? Password { get; set; }
}

public sealed class StudentPortalInvitationAcceptedResponseDTO
{
    public long PortalAccountId { get; init; }
    public string Email { get; init; } = string.Empty;
    public bool RequiresLogin { get; init; }
}

public sealed class StudentPortalPasswordResetRequestDTO
{
    public string Email { get; set; } = string.Empty;
}

public sealed class StudentPortalPasswordResetCompletionRequestDTO
{
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public sealed class StudentPortalPasswordResetResponseDTO
{
    public string Message { get; init; } = string.Empty;
}

public sealed class StudentPortalDocumentUploadDTO
{
    [Range(1, long.MaxValue)]
    public long RequirementId { get; set; }
    public StudentDocumentPersonScope PersonScope { get; set; }
    public long? GuardianId { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    [Required]
    public IFormFile File { get; set; } = null!;
}

public sealed class StudentPortalDocumentFilterDTO
{
    public StudentPortalDocumentFilterDTO()
    {
        PageNumber = PaginationDefaults.PageNumber;
        PageSize = PaginationDefaults.PageSize;
    }

    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public StudentDocumentPersonScope? PersonScope { get; set; }
    public StudentDocumentReviewStatus? Status { get; set; }
}

/// <summary>Pagination for Guardian fee records; tenant and portal identity come from the bearer token.</summary>
public sealed class StudentPortalFeeFilterDTO
{
    public int PageNumber { get; set; } = PaginationDefaults.PageNumber;
    public int PageSize { get; set; } = PaginationDefaults.PageSize;
}

public sealed class StudentPortalFeeAdmissionResponseDTO
{
    public long AdmissionId { get; set; }
    public long AcademicSessionId { get; set; }
    public string AcademicSessionName { get; set; } = string.Empty;
    public long CampusId { get; set; }
    public string CampusName { get; set; } = string.Empty;
    public long AcademicGroupId { get; set; }
    public string AcademicGroupName { get; set; } = string.Empty;
    public long QuoteId { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal NetAnnualAmount { get; set; }
}

public sealed class StudentPortalCashfreeCheckoutRequestDTO
{
    [Required, StringLength(100)]
    public string IdempotencyKey { get; set; } = string.Empty;
}

public sealed class StudentRecordDocumentResponseDTO
{
    public long Id { get; init; }
    public long RequirementId { get; init; }
    public string RequirementCode { get; init; } = string.Empty;
    public string RequirementName { get; init; } = string.Empty;
    public long StudentId { get; init; }
    public StudentDocumentPersonScope PersonScope { get; init; }
    public long? GuardianId { get; init; }
    public int VersionNumber { get; init; }
    public long? SupersedesDocumentId { get; init; }
    public string OriginalFileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long FileSizeBytes { get; init; }
    public string ChecksumSha256 { get; init; } = string.Empty;
    public DateOnly? ExpiryDate { get; init; }
    public StudentDocumentReviewStatus Status { get; init; }
    public DateTime SubmittedDateTime { get; init; }
    public DateTime? ReviewedDateTime { get; init; }
    public string? ReviewReason { get; init; }
}

public sealed class StudentPortalDocumentRequirementResponseDTO
{
    public long Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public StudentDocumentPersonScope AppliesTo { get; init; }
    public bool IsRequired { get; init; }
    public IReadOnlyList<string> AllowedMimeTypes { get; init; } = [];
    public long MaxFileSizeBytes { get; init; }
    public bool RequiresExpiryDate { get; init; }
}

public sealed class StudentPortalSessionResponseDTO
{
    public string UserType { get; init; } = string.Empty;
    public long InstitutionId { get; init; }
    public long PortalAccountId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public DateTime AccessTokenExpiresAtUtc { get; init; }
    public DateTime RefreshTokenExpiresAtUtc { get; init; }
}

public sealed class StudentPortalSessionRevokedResponseDTO
{
    public bool Revoked { get; init; }
}

public sealed class StudentPortalProfileResponseDTO
{
    public long PortalAccountId { get; init; }
    public long InstitutionId { get; init; }
    public string Email { get; init; } = string.Empty;
    public IReadOnlyList<StudentPortalPersonaResponseDTO> Personas { get; init; } = [];
}

public sealed class StudentPortalPersonaResponseDTO
{
    public StudentPortalPersonaType Type { get; init; }
    public long Id { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string? StudentCode { get; init; }
    public IReadOnlyList<StudentPortalLinkedStudentResponseDTO> LinkedStudents { get; init; } = [];
}

public sealed class StudentPortalLinkedStudentResponseDTO
{
    public long StudentId { get; init; }
    public string StudentCode { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
}
