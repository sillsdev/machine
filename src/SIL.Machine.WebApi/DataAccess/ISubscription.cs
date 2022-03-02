namespace SIL.Machine.WebApi.DataAccess;

public interface ISubscription<T> : IDisposable where T : IEntity<T>
{
	EntityChange<T> Change { get; }
	Task WaitForUpdateAsync(TimeSpan? timeout = default, CancellationToken cancellationToken = default);
}
