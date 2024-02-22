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

public record Build
{
    public required string BuildId { get; init; }
    public required BuildJobState JobState { get; init; }
    public required string JobId { get; init; }
    public required BuildJobRunner JobRunner { get; init; }
    public required string Stage { get; init; }
    public string? Options { get; set; }
}
