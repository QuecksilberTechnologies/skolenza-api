using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class ClientType
{
    public int Id { get; set; }

    public string TypeName { get; set; } = null!;

    public bool IsActive { get; set; }

    public string? Remark { get; set; }

    public string? Description { get; set; }

    public virtual ICollection<Client> Client { get; set; } = new List<Client>();

    public virtual ICollection<Tender> Tender { get; set; } = new List<Tender>();
}
