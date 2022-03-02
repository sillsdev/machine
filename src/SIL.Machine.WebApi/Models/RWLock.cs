namespace SIL.Machine.WebApi.Models;

public class RWLock : IEntity<RWLock>
{
	public RWLock()
	{
	}

	public RWLock(RWLock rwLock)
	{
		Id = rwLock.Id;
		Revision = rwLock.Revision;
		WriterLock = rwLock.WriterLock.Clone();
		ReaderLocks = rwLock.ReaderLocks.Select(l => l.Clone()).ToList();
		ReaderCount = rwLock.ReaderCount;
	}

	public string Id { get; set; } = default!;
	public int Revision { get; set; }
	public Lock WriterLock { get; set; } = new Lock();
	public List<Lock> ReaderLocks { get; set; } = new List<Lock>();
	public int ReaderCount { get; set; }

	public bool IsAvailableForReading
	{
		get
		{
			var now = DateTime.UtcNow;
			return !WriterLock.IsAcquired || (WriterLock.ExpiresAt != null && WriterLock.ExpiresAt <= now);
		}
	}

	public bool IsAvailableForWriting
	{
		get
		{
			var now = DateTime.UtcNow;
			return (!WriterLock.IsAcquired || WriterLock.ExpiresAt <= now)
				&& (ReaderCount == 0 || !ReaderLocks.Any(l => l.ExpiresAt == null || l.ExpiresAt > now));
		}
	}

	public RWLock Clone()
	{
		return new RWLock(this);
	}
}
