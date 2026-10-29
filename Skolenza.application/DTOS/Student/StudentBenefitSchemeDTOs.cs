using System.ComponentModel.DataAnnotations;
using skolenza.application.DTOs.BaseDTO;
using skolenza.application.DTOS.Pagination;

namespace skolenza.application.DTOS.Student;

public sealed class StudentBenefitSchemeFilterDTO : PermissionPagedRequestDTO
{
    public StudentBenefitSchemeFilterDTO()
    {
        PageNumber = PaginationDefaults.PageNumber;
        PageSize = PaginationDefaults.PageSize;
    }

    public int? CountryId { get; set; }
    public int? StateId { get; set; }
    public long? CampusId { get; set; }
    public long? AcademicSessionId { get; set; }
    public bool ActiveOnly { get; set; } = true;
}

public sealed class StudentBenefitSchemeRequestDTO : PermissionRequestDTO
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? CountryId { get; set; }
    public int? StateId { get; set; }
    public long? CampusId { get; set; }
    public long? AcademicSessionId { get; set; }

    [Range(typeof(decimal), "0", "100")]
    public decimal MinimumDiscountPercent { get; set; }

    [Range(typeof(decimal), "0", "100")]
    public decimal MaximumDiscountPercent { get; set; }

    public string? EligibilityDescription { get; set; }
    public long? EvidenceRequirementId { get; set; }
    public short DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class StudentBenefitSchemeResponseDTO
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? CountryId { get; set; }
    public string? CountryName { get; set; }
    public int? StateId { get; set; }
    public string? StateName { get; set; }
    public long? CampusId { get; set; }
    public long? AcademicSessionId { get; set; }
    public decimal MinimumDiscountPercent { get; set; }
    public decimal MaximumDiscountPercent { get; set; }
    public string? EligibilityDescription { get; set; }
    public long? EvidenceRequirementId { get; set; }
    public short DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}
