using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class Client
{
    public int Id { get; set; }

    public int ClientTypeId { get; set; }

    public string ClientName { get; set; } = null!;

    public string? ContactPerson { get; set; }

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Address { get; set; }

    public bool IsActive { get; set; }

    public string? Remark { get; set; }

    public virtual ClientType ClientType { get; set; } = null!;
}
