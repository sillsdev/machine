namespace SIL.Machine.WebApi.Services;

public interface IDistributedReaderWriterLockFactory
{
	IDistributedReaderWriterLock Create(string id);
	Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
