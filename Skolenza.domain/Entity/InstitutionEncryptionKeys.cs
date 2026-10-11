using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class InstitutionEncryptionKeys
{
    public long Id { get; set; }

    public long InstitutionId { get; set; }

    public string EncryptionKey { get; set; } = null!;

    public bool? IsActive { get; set; }

    public DateTime? AddedDateTime { get; set; }

    public DateTime? UpdatedDateTime { get; set; }
}
