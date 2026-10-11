using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class TravelMode
{
    public int Id { get; set; }

    public string TravelModeName { get; set; } = null!;

    public string? Description { get; set; }

    public bool? IsActive { get; set; }

    public int? AddedById { get; set; }

    public DateTime? AddedDateTime { get; set; }

    public int? UpdatedById { get; set; }

    public DateTime? UpdatedDateTime { get; set; }
}
