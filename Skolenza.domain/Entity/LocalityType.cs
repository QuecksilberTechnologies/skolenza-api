namespace skolenza.domain.Entity;

/// <summary>Classifies a locality as a city, town, or village.</summary>
public partial class LocalityType
{
    public int Id { get; set; }

    public string TypeName { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual ICollection<Locality> Localities { get; set; } = new List<Locality>();
}
