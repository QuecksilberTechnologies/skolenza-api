using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class Tender
{
    public int Id { get; set; }

    public int ClientId { get; set; }

    public int TenderStatusId { get; set; }

    public string TenderName { get; set; } = null!;

    public string? Description { get; set; }

    public string? Remark { get; set; }

    public decimal TenderValue { get; set; }

    public DateOnly? EndDate { get; set; }

    public DateOnly StartDate { get; set; }

    public bool IsActive { get; set; }

    public virtual ClientType Client { get; set; } = null!;

    public virtual TenderStatus TenderStatus { get; set; } = null!;
}
