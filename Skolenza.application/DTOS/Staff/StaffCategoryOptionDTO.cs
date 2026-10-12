namespace skolenza.application.DTOS.Staff;

/// <summary>One active platform Staff Category option.</summary>
public sealed record StaffCategoryOptionDTO(
    short Id,
    string Code,
    string Label,
    string? Description);
