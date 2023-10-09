namespace SIL.Machine.AspNetCore.Models;

public enum BuildJobState
{
    None,
    Pending,
    Active,
    Canceling
}

public enum BuildJobRunner
{
    Hangfire,
    ClearML
}

public class Build
{
    public string BuildId { get; set; } = default!;
    public BuildJobState JobState { get; set; }
    public string JobId { get; set; } = default!;
    public BuildJobRunner JobRunner { get; set; }
    public string Stage { get; set; } = default!;
    public string? Options { get; set; } = default;
}
