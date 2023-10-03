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

public class ClearMLTask
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public ClearMLProject Project { get; set; } = default!;
    public ClearMLTaskStatus Status { get; set; }
    public string StatusReason { get; set; } = default!;
    public string StatusMessage { get; set; } = default!;
    public int LastIteration { get; set; }
    public int ActiveDuration { get; set; }
}
