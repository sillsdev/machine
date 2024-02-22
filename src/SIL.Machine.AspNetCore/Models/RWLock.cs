namespace SIL.Machine.AspNetCore.Models;

public record RWLock : IEntity
{
    public string Id { get; set; } = "";
    public int Revision { get; set; } = 1;
    public Lock? WriterLock { get; init; }
    public required IReadOnlyList<Lock> ReaderLocks { get; init; }
    public required IReadOnlyList<Lock> WriterQueue { get; init; }

    public bool IsAvailableForReading()
    {
        var now = DateTime.UtcNow;
        return (WriterLock is null || WriterLock.ExpiresAt is not null && WriterLock.ExpiresAt <= now)
            && WriterQueue.Count == 0;
    }

    public bool IsAvailableForWriting(string? lockId = null)
    {
        var now = DateTime.UtcNow;
        return (WriterLock is null || WriterLock.ExpiresAt is not null && WriterLock.ExpiresAt <= now)
            && !ReaderLocks.Any(l => l.ExpiresAt is null || l.ExpiresAt > now)
            && (lockId is null || WriterQueue.FirstOrDefault()?.Id == lockId);
    }
}
