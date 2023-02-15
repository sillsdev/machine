namespace SIL.Machine.AspNetCore.Services;

public class DistributedReaderWriterLockFactory : IDistributedReaderWriterLockFactory
{
    private readonly ServiceOptions _serviceOptions;
    private readonly IRepository<RWLock> _locks;

    public DistributedReaderWriterLockFactory(IOptions<ServiceOptions> serviceOptions, IRepository<RWLock> locks)
    {
        _serviceOptions = serviceOptions.Value;
        _locks = locks;
    }

    public async Task InitAsync()
    {
        await RemoveAllWaitersAsync();
        await ReleaseAllWriterLocksAsync();
        await ReleaseAllReaderLocksAsync();
    }

    public IDistributedReaderWriterLock Create(string id)
    {
        return new DistributedReaderWriterLock(_serviceOptions.ServiceId, _locks, id);
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        RWLock? rwLock = await _locks.DeleteAsync(id, cancellationToken);
        return rwLock is not null;
    }

    private async Task ReleaseAllWriterLocksAsync()
    {
        string hostId = _serviceOptions.ServiceId;
        IReadOnlyList<RWLock> rwLocks = await _locks.GetAllAsync(
            rwl => rwl.WriterLock != null && rwl.WriterLock.HostId == hostId
        );
        var tasks = new List<Task>();
        foreach (RWLock rwLock in rwLocks)
            tasks.Add(_locks.UpdateAsync(rwLock, u => u.Unset(rwl => rwl.WriterLock)));
        await Task.WhenAll(tasks);
    }

    private async Task ReleaseAllReaderLocksAsync()
    {
        string hostId = _serviceOptions.ServiceId;
        IReadOnlyList<RWLock> rwLocks = await _locks.GetAllAsync(rwl => rwl.ReaderLocks.Any(l => l.HostId == hostId));
        var tasks = new List<Task>();
        foreach (RWLock rwLock in rwLocks)
        {
            tasks.Add(_locks.UpdateAsync(rwLock, u => u.RemoveAll(rwl => rwl.ReaderLocks, l => l.HostId == hostId)));
        }
        await Task.WhenAll(tasks);
    }

    private async Task RemoveAllWaitersAsync()
    {
        string hostId = _serviceOptions.ServiceId;
        IReadOnlyList<RWLock> rwLocks = await _locks.GetAllAsync(rwl => rwl.WriterQueue.Any(l => l.HostId == hostId));
        var tasks = new List<Task>();
        foreach (RWLock rwLock in rwLocks)
        {
            tasks.Add(_locks.UpdateAsync(rwLock, u => u.RemoveAll(rwl => rwl.WriterQueue, l => l.HostId == hostId)));
        }
        await Task.WhenAll(tasks);
    }
}
