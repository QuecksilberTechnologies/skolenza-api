using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class CandidateCategorySkill
{
    public int Id { get; set; }

    public long CandidateId { get; set; }

    public int CategoryId { get; set; }

    public string? Description { get; set; }

    public DateTime AddedDateTime { get; set; }

    public bool IsActive { get; set; }

    public virtual Candidate Candidate { get; set; } = null!;

    public virtual Category Category { get; set; } = null!;
}
