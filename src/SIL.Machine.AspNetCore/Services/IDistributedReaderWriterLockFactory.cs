namespace SIL.Machine.AspNetCore.Services;

public interface IDistributedReaderWriterLockFactory
{
    Task InitAsync(CancellationToken cancellationToken = default);
    IDistributedReaderWriterLock Create(string id);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
