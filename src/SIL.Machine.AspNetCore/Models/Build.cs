namespace SIL.Machine.AspNetCore.Models;

public enum BuildJobState
{
    None,
    Pending,
    Active,
    Canceling
}

public enum BuildJobRunnerType
{
    Hangfire,
    ClearML
}

public enum BuildStage
{
    Preprocess,
    Train,
    Postprocess
}

public record Build
{
    public required string BuildId { get; init; }
    public required BuildJobState JobState { get; init; }
    public required string JobId { get; init; }
    public required BuildJobRunnerType BuildJobRunner { get; init; }
    public required BuildStage Stage { get; init; }
    public string? Options { get; set; }
}
