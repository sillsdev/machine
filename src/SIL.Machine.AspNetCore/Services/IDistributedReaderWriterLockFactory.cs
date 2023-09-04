namespace SIL.Machine.AspNetCore.Services;

public interface IDistributedReaderWriterLockFactory
{
    Task InitAsync(CancellationToken cancellationToken = default);
    Task<IDistributedReaderWriterLock> CreateAsync(string id, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
