namespace SIL.Machine.WebApi.Utils;

public interface IDistributedReaderWriterLock
{
	ValueTask<IAsyncDisposable> ReaderLockAsync(TimeSpan? timeout = default, TimeSpan? lifetime = default,
		CancellationToken cancellationToken = default);
	ValueTask<IAsyncDisposable> WriterLockAsync(TimeSpan? timeout = default, TimeSpan? lifetime = default,
		CancellationToken cancellationToken = default);
}
