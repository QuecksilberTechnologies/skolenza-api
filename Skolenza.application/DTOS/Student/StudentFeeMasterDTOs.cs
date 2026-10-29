using System.ComponentModel.DataAnnotations;
using skolenza.application.Common.Enums;
using skolenza.application.DTOs.BaseDTO;
using skolenza.application.DTOS.Pagination;

namespace skolenza.application.DTOS.Student;

public abstract class StudentFeeMasterFilterDTO : PermissionPagedRequestDTO
{
    protected StudentFeeMasterFilterDTO()
    {
        PageNumber = PaginationDefaults.PageNumber;
        PageSize = PaginationDefaults.PageSize;
    }

    public bool ActiveOnly { get; set; } = true;
}

public sealed class StudentFeeFrequencyFilterDTO : StudentFeeMasterFilterDTO
{
}

public sealed class StudentFeeFrequencyRequestDTO : PermissionRequestDTO
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Range(1, 12)]
    public short InstallmentsPerYear { get; set; }

    public short DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

public sealed class StudentFeeFrequencyResponseDTO
{
    public long Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public short InstallmentsPerYear { get; set; }

    public short DisplayOrder { get; set; }

    public bool IsActive { get; set; }
}

public sealed class StudentFeeComponentFilterDTO : StudentFeeMasterFilterDTO
{
    public StudentFeeComponentChargeNature? ChargeNature { get; set; }
}

public sealed class StudentFeeComponentRequestDTO : PermissionRequestDTO
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public StudentFeeComponentChargeNature ChargeNature { get; set; }

    public short DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

public sealed class StudentFeeComponentResponseDTO
{
    public long Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public StudentFeeComponentChargeNature ChargeNature { get; set; }

    public short DisplayOrder { get; set; }

    public bool IsActive { get; set; }
}

public sealed class StudentFeeMasterDeleteDTO : PermissionRequestDTO
{
}
