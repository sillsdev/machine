namespace SIL.Machine.WebApi.Services;

public interface IDistributedReaderWriterLockFactory
{
	IDistributedReaderWriterLock Create(string id);
	ValueTask<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
}
