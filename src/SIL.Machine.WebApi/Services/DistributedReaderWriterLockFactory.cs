namespace SIL.Machine.WebApi.Services;

public class DistributedReaderWriterLockFactory : IDistributedReaderWriterLockFactory
{
	private readonly IOptions<ServiceOptions> _serviceOptions;
	private readonly IRepository<RWLock> _locks;

	public DistributedReaderWriterLockFactory(IOptions<ServiceOptions> serviceOptions, IRepository<RWLock> locks)
	{
		_serviceOptions = serviceOptions;
		_locks = locks;
	}

	public IDistributedReaderWriterLock Create(string id)
	{
		return new DistributedReaderWriterLock(_serviceOptions.Value.HostId, _locks, id);
	}

	public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
	{
		RWLock? rwLock = await _locks.DeleteAsync(id, cancellationToken);
		return rwLock is not null;
	}
}
