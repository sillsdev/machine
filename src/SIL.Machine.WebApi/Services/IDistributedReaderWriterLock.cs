namespace SIL.Machine.WebApi.Services;

public interface IDistributedReaderWriterLock
{
	Task<IAsyncDisposable> ReaderLockAsync(TimeSpan? lifetime = default,
		CancellationToken cancellationToken = default);
	Task<IAsyncDisposable> WriterLockAsync(TimeSpan? lifetime = default,
		CancellationToken cancellationToken = default);
}
