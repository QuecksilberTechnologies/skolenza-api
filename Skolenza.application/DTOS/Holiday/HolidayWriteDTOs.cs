using skolenza.application.DTOs.BaseDTO;
using skolenza.application.DTOS.Common;

namespace skolenza.application.DTOs.Holiday;

public sealed class HolidayByIdRequestDTO : PermissionRequestDTO
{
    public long Id { get; set; }
}

public sealed class SaveHolidayRequestDTO : PermissionRequestDTO
{
    public long CampusId { get; set; }

    public string HolidayName { get; set; } = string.Empty;

    public DateOnly HolidayDate { get; set; }

    public bool IsOptional { get; set; }

    public string? Description { get; set; }

    public string? Icon { get; set; }
}

public sealed class UpdateHolidayRequestDTO : PermissionRequestDTO
{
    public long Id { get; set; }

    public long CampusId { get; set; }

    public string HolidayName { get; set; } = string.Empty;

    public DateOnly HolidayDate { get; set; }

    public bool IsOptional { get; set; }

    public string? Description { get; set; }

    public string? Icon { get; set; }
}

public sealed class ImportHolidayRequestDTO : BulkImportPreviewRequestDTO
{
}

public sealed class HolidayImportResultDTO
{
    public int TotalRows { get; set; }

    public int CreatedCount { get; set; }

    public int SkippedExistingCount { get; set; }
}
