namespace SIL.Machine.WebApi.Models;

public class RWLock : IEntity
{
	public string Id { get; set; } = default!;
	public int Revision { get; set; }
	public Lock? WriterLock { get; set; }
	public List<Lock> ReaderLocks { get; set; } = new List<Lock>();

	public bool IsAvailableForReading
	{
		get
		{
			var now = DateTime.UtcNow;
			return WriterLock == null || (WriterLock.ExpiresAt != null && WriterLock.ExpiresAt <= now);
		}
	}

	public bool IsAvailableForWriting
	{
		get
		{
			var now = DateTime.UtcNow;
			return (WriterLock == null || (WriterLock.ExpiresAt != null && WriterLock.ExpiresAt <= now))
				&& !ReaderLocks.Any(l => l.ExpiresAt == null || l.ExpiresAt > now);
		}
	}
}
