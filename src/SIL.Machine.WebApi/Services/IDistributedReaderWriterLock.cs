namespace SIL.Machine.WebApi.Services;

public interface IDistributedReaderWriterLock
{
	ValueTask<IAsyncDisposable> ReaderLockAsync(TimeSpan? lifetime = default,
		CancellationToken cancellationToken = default);
	ValueTask<IAsyncDisposable> WriterLockAsync(TimeSpan? lifetime = default,
		CancellationToken cancellationToken = default);
}
