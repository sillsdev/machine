namespace SIL.Machine.WebApi.Services;

public class DistributedReaderWriterLockFactory : IDistributedReaderWriterLockFactory
{
	private readonly IRepository<RWLock> _locks;

	public DistributedReaderWriterLockFactory(IRepository<RWLock> locks)
	{
		_locks = locks;
	}

	public IDistributedReaderWriterLock Create(string id)
	{
		return new DistributedReaderWriterLock(_locks, id);
	}

	public async ValueTask<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
	{
		RWLock? rwLock = await _locks.DeleteAsync(id, cancellationToken);
		return rwLock is not null;
	}
}
