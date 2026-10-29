using System.ComponentModel.DataAnnotations;
using skolenza.application.DTOs.BaseDTO;
using skolenza.application.DTOS.Pagination;

namespace skolenza.application.DTOS.Student;

public sealed class StudentEducationIdentifierMasterFilterDTO : PermissionPagedRequestDTO
{
    public StudentEducationIdentifierMasterFilterDTO()
    {
        PageNumber = PaginationDefaults.PageNumber;
        PageSize = PaginationDefaults.PageSize;
    }

    public int? CountryId { get; set; }
    public bool ActiveOnly { get; set; } = true;
}

public sealed class StudentEducationIdentifierMasterRequestDTO : PermissionRequestDTO
{
    public int CountryId { get; set; }

    [Required, StringLength(150)]
    public string IssuingAuthority { get; set; } = string.Empty;

    [Required, StringLength(60)]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public short DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class StudentEducationIdentifierMasterResponseDTO
{
    public long Id { get; set; }
    public int CountryId { get; set; }
    public string CountryName { get; set; } = string.Empty;
    public string IssuingAuthority { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public short DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}

public sealed class StudentEducationIdentifierMasterDeleteDTO : PermissionRequestDTO
{
}
