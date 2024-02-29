namespace SIL.Machine.AspNetCore.Models;

public enum ClearMLTaskStatus
{
    Created,
    Queued,
    InProgress,
    Stopped,
    Published,
    Publishing,
    Closed,
    Failed,
    Completed,
    Unknown
}

public record ClearMLTask
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required ClearMLProject Project { get; init; }
    public required ClearMLTaskStatus Status { get; init; }
    public string? StatusReason { get; init; }
    public string? StatusMessage { get; init; }
    public required DateTime Created { get; init; }
    public int? LastIteration { get; init; }
    public int ActiveDuration { get; init; }
    public required IReadOnlyDictionary<
        string,
        IReadOnlyDictionary<string, ClearMLMetricsEvent>
    > LastMetrics { get; init; }
}
