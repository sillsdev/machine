namespace SIL.Machine.WebApi.DataAccess;

public class MemoryDistributedReaderWriterLock : IDistributedReaderWriterLock
{
	private readonly AsyncReaderWriterLock _lock;

	public MemoryDistributedReaderWriterLock()
	{
		_lock = new AsyncReaderWriterLock();
	}

	public async ValueTask<IAsyncDisposable> ReaderLockAsync(TimeSpan? timeout = default, TimeSpan? lifetime = default,
		CancellationToken cancellationToken = default)
	{
		timeout ??= Timeout.InfiniteTimeSpan;
		IDisposable? releaser = await _lock.ReaderLockAsync(cancellationToken).AsTask()
			.Timeout(timeout.GetValueOrDefault(), cancellationToken);
		return new AsyncDisposableWrapper(releaser);
	}

	public async ValueTask<IAsyncDisposable> WriterLockAsync(TimeSpan? timeout = default, TimeSpan? lifetime = default,
		CancellationToken cancellationToken = default)
	{
		timeout ??= Timeout.InfiniteTimeSpan;
		IDisposable? releaser = await _lock.WriterLockAsync(cancellationToken).AsTask()
			.Timeout(timeout.GetValueOrDefault(), cancellationToken);
		return new AsyncDisposableWrapper(releaser);
	}
}
