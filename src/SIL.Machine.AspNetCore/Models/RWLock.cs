namespace SIL.Machine.AspNetCore.Models;

public class RWLock : IEntity
{
    public string Id { get; set; } = default!;
    public int Revision { get; set; }
    public Lock? WriterLock { get; set; }
    public List<Lock> ReaderLocks { get; set; } = [];
    public List<Lock> WriterQueue { get; set; } = [];

    public bool IsAvailableForReading()
    {
        DateTime now = DateTime.UtcNow;
        return (WriterLock is null || WriterLock.ExpiresAt is not null && WriterLock.ExpiresAt <= now)
            && WriterQueue.Count == 0;
    }

    public bool IsAvailableForWriting(string? lockId = null)
    {
        DateTime now = DateTime.UtcNow;
        return (WriterLock is null || WriterLock.ExpiresAt is not null && WriterLock.ExpiresAt <= now)
            && !ReaderLocks.Any(l => l.ExpiresAt is null || l.ExpiresAt > now)
            && (lockId is null || WriterQueue.FirstOrDefault()?.Id == lockId);
    }
}
