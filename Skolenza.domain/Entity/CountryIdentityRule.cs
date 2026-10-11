using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class CountryIdentityRule
{
    public int Id { get; set; }

    public int CountryId { get; set; }

    public int IdentityCategoryDocumentId { get; set; }

    public bool IsMandatory { get; set; }

    public bool IsActive { get; set; }

    public long? AddedById { get; set; }

    public long? UpdatedById { get; set; }

    public DateTime AddedDateTime { get; set; }

    public DateTime? UpdatedDateTime { get; set; }

    public virtual Country Country { get; set; } = null!;

    public virtual IdentityCategoryDocument IdentityCategoryDocument { get; set; } = null!;
}
