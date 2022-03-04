namespace SIL.Machine.WebApi.DataAccess;

public class MemorySubscription<T> : DisposableBase, ISubscription<T> where T : IEntity<T>
{
	private readonly Action<MemorySubscription<T>> _remove;
	private readonly AsyncAutoResetEvent _changeEvent;

	public MemorySubscription(T? initialEntity, Action<MemorySubscription<T>> remove)
	{
		_remove = remove;
		_changeEvent = new AsyncAutoResetEvent();
		Change = new EntityChange<T>(initialEntity == null ? EntityChangeType.Delete : EntityChangeType.Update,
			initialEntity);
	}

	public EntityChange<T> Change { get; private set; }

	public async Task WaitForUpdateAsync(TimeSpan? timeout = default, CancellationToken cancellationToken = default)
	{
		if (timeout is null)
			timeout = Timeout.InfiniteTimeSpan;
		await TaskEx.Timeout(ct => _changeEvent.WaitAsync(ct), timeout.Value, cancellationToken);
	}

	internal void HandleChange(EntityChange<T> change)
	{
		Change = change;
		_changeEvent.Set();
	}

	protected override void DisposeManagedResources()
	{
		_remove(this);
	}
}
