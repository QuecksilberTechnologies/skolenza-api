using System.ComponentModel.DataAnnotations;
using skolenza.application.Common.Enums;
using skolenza.application.DTOs.BaseDTO;
using skolenza.application.DTOS.Pagination;

namespace skolenza.application.DTOS.Student;

public sealed class StudentEducationIdentifierFilterDTO : PermissionPagedRequestDTO
{
    public StudentEducationIdentifierFilterDTO()
    {
        PageNumber = PaginationDefaults.PageNumber;
        PageSize = PaginationDefaults.PageSize;
    }

    public long StudentId { get; set; }
}

public sealed class StudentEducationIdentifierRequestDTO : PermissionRequestDTO
{
    public long StudentId { get; set; }
    public long IdentifierMasterId { get; set; }

    [Required, StringLength(80)]
    public string Value { get; set; } = string.Empty;

    [StringLength(150)]
    public string? IssuerReference { get; set; }

    public DateOnly? IssuedOn { get; set; }
}

public sealed class StudentEducationIdentifierUpdateDTO : PermissionRequestDTO
{
    [Required, StringLength(80)]
    public string Value { get; set; } = string.Empty;

    [StringLength(150)]
    public string? IssuerReference { get; set; }

    public DateOnly? IssuedOn { get; set; }
}

public sealed class StudentEducationIdentifierVerificationDTO : PermissionRequestDTO
{
    [EnumDataType(typeof(StudentEducationIdentifierVerificationStatus))]
    public short VerificationStatus { get; set; }

    [StringLength(500)]
    public string? Reason { get; set; }
}

public sealed class StudentEducationIdentifierResponseDTO
{
    public long Id { get; set; }
    public long StudentId { get; set; }
    public long IdentifierMasterId { get; set; }
    public int CountryId { get; set; }
    public string CountryName { get; set; } = string.Empty;
    public string IdentifierCode { get; set; } = string.Empty;
    public string IdentifierName { get; set; } = string.Empty;
    public string IssuingAuthority { get; set; } = string.Empty;
    public string MaskedValue { get; set; } = string.Empty;
    public short VerificationStatus { get; set; }
    public string? IssuerReference { get; set; }
    public DateOnly? IssuedOn { get; set; }
    public long AddedById { get; set; }
    public DateTime AddedDateTime { get; set; }
    public long? VerifiedById { get; set; }
    public DateTime? VerifiedDateTime { get; set; }
    public long? UpdatedById { get; set; }
    public DateTime? UpdatedDateTime { get; set; }
}
