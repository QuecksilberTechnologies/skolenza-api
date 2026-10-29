using System.ComponentModel.DataAnnotations;
using skolenza.application.Common.Enums;
using skolenza.application.DTOs.BaseDTO;
using skolenza.application.DTOS.Pagination;

namespace skolenza.application.DTOS.Student;

public sealed class StudentDocumentRequirementFilterDTO : PermissionPagedRequestDTO
{
    public StudentDocumentRequirementFilterDTO()
    {
        PageNumber = PaginationDefaults.PageNumber;
        PageSize = PaginationDefaults.PageSize;
    }

    public long? CampusId { get; set; }
    public long? AcademicSessionId { get; set; }
    public bool ActiveOnly { get; set; } = true;
    public StudentDocumentPersonScope? AppliesTo { get; set; }
}

public sealed class StudentDocumentRequirementRequestDTO : PermissionRequestDTO
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public long? CampusId { get; set; }

    public long? AcademicSessionId { get; set; }

    public StudentDocumentPersonScope AppliesTo { get; set; }

    public bool IsRequired { get; set; }

    public IReadOnlyList<string> AllowedMimeTypes { get; set; } = [];

    [Range(1, 104857600)]
    public long MaxFileSizeBytes { get; set; } = 10485760;

    public bool RequiresExpiryDate { get; set; }

    public short DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

public sealed class StudentDocumentRequirementResponseDTO
{
    public long Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public long? CampusId { get; set; }

    public long? AcademicSessionId { get; set; }

    public StudentDocumentPersonScope AppliesTo { get; set; }

    public bool IsRequired { get; set; }

    public IReadOnlyList<string> AllowedMimeTypes { get; set; } = [];

    public long MaxFileSizeBytes { get; set; }

    public bool RequiresExpiryDate { get; set; }

    public short DisplayOrder { get; set; }

    public bool IsActive { get; set; }
}

public sealed class StudentDocumentRequirementDeleteDTO : PermissionRequestDTO
{
    public long Id { get; set; }
}
