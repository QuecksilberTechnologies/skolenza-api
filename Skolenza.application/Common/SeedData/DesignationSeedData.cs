namespace skolenza.application.Common.SeedData
{
    /// <summary>
    /// Structure used for seeding Designation master data.
    /// DepartmentName se runtime pe DepartmentId map hoga.
    /// </summary>
    public class DesignationSeedData
    {
        public string DesignationName { get; set; } = string.Empty;

        public string DepartmentName { get; set; } = string.Empty; // runtime mapping
        public string Description { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public bool IsExecutive { get; set; } = false; // optional, if applicable

        // Optional extra fields if needed later
        // public long? AddedById { get; set; }
        // public DateTime? AddedDateTime { get; set; }
    }
}
