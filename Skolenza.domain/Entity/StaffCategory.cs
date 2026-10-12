namespace skolenza.domain.Entity;

/// <summary>Defines the platform StaffMember classification catalogue.</summary>
public sealed class StaffCategory
{
    public short Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public short DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public bool IsSystemDefined { get; set; }
    public DateTime AddedDateTime { get; set; }
    public DateTime? UpdatedDateTime { get; set; }
    public ICollection<StaffMember> StaffMembers { get; set; } = new List<StaffMember>();
}
