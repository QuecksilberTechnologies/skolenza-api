using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using skolenza.application.Common.Enums;
using skolenza.application.DTOs.BaseDTO;
using skolenza.application.DTOS.Pagination;

namespace skolenza.application.DTOS.Student;

public sealed class StudentAdmissionDocumentUploadDTO : PermissionRequestDTO
{
    [Range(1, long.MaxValue)]
    public long RequirementId { get; set; }

    public StudentDocumentPersonScope PersonScope { get; set; }

    public long? GuardianId { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    [Required]
    public IFormFile File { get; set; } = null!;
}

public sealed class StudentAdmissionDocumentFilterDTO : PermissionPagedRequestDTO
{
    public StudentAdmissionDocumentFilterDTO()
    {
        PageNumber = PaginationDefaults.PageNumber;
        PageSize = PaginationDefaults.PageSize;
    }

    public StudentDocumentPersonScope? PersonScope { get; set; }

    public long? GuardianId { get; set; }

    public StudentDocumentReviewStatus? Status { get; set; }
}

public sealed class StudentAdmissionDocumentReviewDTO : PermissionRequestDTO
{
    public StudentDocumentReviewStatus Status { get; set; }

    [MaxLength(1000)]
    public string? Reason { get; set; }
}

public sealed class StudentAdmissionDocumentResponseDTO
{
    public long Id { get; set; }
    public long RequirementId { get; set; }
    public string RequirementCode { get; set; } = string.Empty;
    public string RequirementName { get; set; } = string.Empty;
    public StudentDocumentPersonScope PersonScope { get; set; }
    public long? GuardianId { get; set; }
    public int VersionNumber { get; set; }
    public long? SupersedesDocumentId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string ChecksumSha256 { get; set; } = string.Empty;
    public DateOnly? ExpiryDate { get; set; }
    public StudentDocumentReviewStatus Status { get; set; }
    public long SubmittedById { get; set; }
    public DateTime SubmittedDateTime { get; set; }
    public long? ReviewedById { get; set; }
    public DateTime? ReviewedDateTime { get; set; }
    public string? ReviewReason { get; set; }
}
