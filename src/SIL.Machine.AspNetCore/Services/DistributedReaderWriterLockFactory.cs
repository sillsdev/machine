namespace SIL.Machine.AspNetCore.Services;

public class DistributedReaderWriterLockFactory : IDistributedReaderWriterLockFactory
{
    private readonly ServiceOptions _serviceOptions;
    private readonly IIdGenerator _idGenerator;
    private readonly IRepository<RWLock> _locks;

    public DistributedReaderWriterLockFactory(
        IOptions<ServiceOptions> serviceOptions,
        IRepository<RWLock> locks,
        IIdGenerator idGenerator
    )
    {
        _serviceOptions = serviceOptions.Value;
        _locks = locks;
        _idGenerator = idGenerator;
    }

    public async Task InitAsync(CancellationToken cancellationToken = default)
    {
        await RemoveAllWaitersAsync(cancellationToken);
        await ReleaseAllWriterLocksAsync(cancellationToken);
        await ReleaseAllReaderLocksAsync(cancellationToken);
    }

    public async Task<IDistributedReaderWriterLock> CreateAsync(
        string id,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            await _locks.InsertAsync(new RWLock { Id = id }, cancellationToken);
        }
        catch (DuplicateKeyException)
        {
            // the lock is already made - no new one needs to be made
            // This is done instead of checking if it exists first to prevent race conditions.
        }
        return new DistributedReaderWriterLock(_serviceOptions.ServiceId, _locks, _idGenerator, id);
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        RWLock? rwLock = await _locks.DeleteAsync(rwl => rwl.Id == id, cancellationToken);
        return rwLock is not null;
    }

    private async Task ReleaseAllWriterLocksAsync(CancellationToken cancellationToken)
    {
        await _locks.UpdateAllAsync(
            rwl => rwl.WriterLock != null && rwl.WriterLock.HostId == _serviceOptions.ServiceId,
            u => u.Unset(rwl => rwl.WriterLock),
            cancellationToken
        );
    }

    private async Task ReleaseAllReaderLocksAsync(CancellationToken cancellationToken)
    {
        await _locks.UpdateAllAsync(
            rwl => rwl.ReaderLocks.Any(l => l.HostId == _serviceOptions.ServiceId),
            u => u.RemoveAll(rwl => rwl.ReaderLocks, l => l.HostId == _serviceOptions.ServiceId),
            cancellationToken
        );
    }

    private async Task RemoveAllWaitersAsync(CancellationToken cancellationToken)
    {
        await _locks.UpdateAllAsync(
            rwl => rwl.WriterQueue.Any(l => l.HostId == _serviceOptions.ServiceId),
            u => u.RemoveAll(rwl => rwl.WriterQueue, l => l.HostId == _serviceOptions.ServiceId),
            cancellationToken
        );
    }
}
