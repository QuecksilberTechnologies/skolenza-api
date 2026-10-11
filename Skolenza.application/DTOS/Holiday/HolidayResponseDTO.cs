namespace skolenza.application.DTOs.Holiday;

public sealed class HolidayDTO
{
    public long Id { get; set; }

    public long InstitutionId { get; set; }

    public long CampusId { get; set; }

    public string HolidayName { get; set; } = string.Empty;

    public DateOnly HolidayDate { get; set; }

    public bool IsOptional { get; set; }

    public bool IsActive { get; set; }

    public string? Description { get; set; }

    public string? Icon { get; set; }
}

/// <summary>Describes one stable UI style used to render an staffMember-calendar date.</summary>
public sealed record HolidayCalendarStyleDTO(
    string StatusCode,
    string Label,
    string BackgroundColor,
    string TextColor,
    int Priority);

/// <summary>Returns the shared staffMember-calendar color palette and overlap precedence.</summary>
public sealed record HolidayCalendarDisplayConstantsDTO(
    IReadOnlyList<HolidayCalendarStyleDTO> Styles);
