using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class ServiceProvider
{
    public int Id { get; set; }

    public string CompanyName { get; set; } = null!;

    public string Location { get; set; } = null!;

    public string Address { get; set; } = null!;

    public string PinCode { get; set; } = null!;

    public string? Profile { get; set; }

    public string CompanyEmail { get; set; } = null!;

    public string? Fax { get; set; }

    public string Phone { get; set; } = null!;

    public string Ceoname { get; set; } = null!;

    public string? Gstnumber { get; set; }

    public string? WebsiteUrl { get; set; }

    public int? EstablishedYear { get; set; }

    public string? CompanyType { get; set; }

    public string? Description { get; set; }

    public string? Remark { get; set; }

    public bool? IsActive { get; set; }
}
