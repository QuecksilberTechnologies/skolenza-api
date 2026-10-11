namespace skolenza.application.Common.Models;

/// <summary>One shared worker configuration for every institution. No per-institution hosted services.</summary>
public sealed class BulkImportOptions
{
    public const string SectionName = "BulkImport";
    public bool WorkerEnabled { get; set; }
    public int PollIntervalSeconds { get; set; } = 5;
    public int BatchSize { get; set; } = 50;
    public int BatchTimeoutSeconds { get; set; } = 60;
}
