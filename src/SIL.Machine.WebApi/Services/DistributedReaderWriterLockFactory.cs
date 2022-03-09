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

	public async Task InitAsync()
	{
		await ReleaseAllWriterLocksAsync();
		await ReleaseAllReaderLocksAsync();
	}

	public IDistributedReaderWriterLock Create(string id)
	{
		return new DistributedReaderWriterLock(_serviceOptions.Value.ServiceId, _locks, id);
	}

	public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
	{
		RWLock? rwLock = await _locks.DeleteAsync(id, cancellationToken);
		return rwLock is not null;
	}

	private async Task ReleaseAllWriterLocksAsync()
	{
		IReadOnlyList<RWLock> rwLocks = await _locks.GetAllAsync(rwl =>
			rwl.WriterLock != null && rwl.WriterLock.HostId == _serviceOptions.Value.ServiceId);
		var tasks = new List<Task>();
		foreach (RWLock rwLock in rwLocks)
			tasks.Add(_locks.UpdateAsync(rwLock, u => u.Unset(rwl => rwl.WriterLock)));
		await Task.WhenAll(tasks);
	}

	private async Task ReleaseAllReaderLocksAsync()
	{
		IReadOnlyList<RWLock> rwLocks = await _locks.GetAllAsync(rwl =>
			rwl.ReaderLocks.Any(l => l.HostId == _serviceOptions.Value.ServiceId));
		var tasks = new List<Task>();
		foreach (RWLock rwLock in rwLocks)
		{
			tasks.Add(_locks.UpdateAsync(rwLock, u => u
				.RemoveAll(rwl => rwl.ReaderLocks, l => l.HostId == _serviceOptions.Value.ServiceId)));
		}
		await Task.WhenAll(tasks);
	}
}
