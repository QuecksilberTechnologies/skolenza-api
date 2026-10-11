using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class InstitutionEmailConfig
{
    public int Id { get; set; }

    public long InstitutionId { get; set; }

    public string? SmtpHost { get; set; }

    public int? SmtpPort { get; set; }

    public string? SmtpUsername { get; set; }

    public string? SmtpPasswordEncrypted { get; set; }

    public string? FromEmail { get; set; }

    public string? FromName { get; set; }

    public bool IsActive { get; set; }

    public string? SecrateKey { get; set; }

    public virtual Institution Institution { get; set; } = null!;
}
