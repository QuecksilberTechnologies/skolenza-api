namespace skolenza.domain.Entity;

/// <summary>Represents a city, town, or village within a district.</summary>
public partial class Locality
{
    public int Id { get; set; }

    public string LocalityName { get; set; } = null!;

    public string LocalityCode { get; set; } = null!;

    public string? PostalCode { get; set; }

    public int StateId { get; set; }

    public int DistrictId { get; set; }

    public int LocalityTypeId { get; set; }

    public bool? IsActive { get; set; }

    public virtual State State { get; set; } = null!;

    public virtual District District { get; set; } = null!;

    public virtual LocalityType LocalityType { get; set; } = null!;

}
