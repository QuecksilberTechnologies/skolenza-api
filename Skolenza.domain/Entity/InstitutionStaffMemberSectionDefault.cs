namespace skolenza.domain.Entity;

/// <summary>Institution-wide staffMember self-service defaults for operational sections.</summary>
public sealed class InstitutionStaffMemberSectionDefault
{
    public long InstitutionId { get; set; }
    public string ModuleCode { get; set; } = string.Empty;
    public bool IsEditAllowed { get; set; }
    public long UpdatedById { get; set; }
    public DateTime UpdatedDateTime { get; set; }
}
