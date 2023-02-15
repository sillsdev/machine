namespace SIL.Machine.AspNetCore.Services;

public interface IDistributedReaderWriterLockFactory
{
    Task InitAsync();
    IDistributedReaderWriterLock Create(string id);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
